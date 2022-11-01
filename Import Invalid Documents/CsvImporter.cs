using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Import_Invalid_Documents
{
    /// <summary>
    /// Třída, která má na starosti import dat
    /// </summary>
    public class CsvImporter
    {
        /// <summary>
        /// Uživatelské jméno pro příhlášení do CRM
        /// </summary>
        private readonly string userName;
        /// <summary>
        /// Heslo pro přihlášení do CRM
        /// </summary>
        private readonly string password;
        /// <summary>
        /// počet simultánních požadavků na server
        /// </summary>
        private readonly int parallelTasks;
        /// <summary>
        /// adresa API prostředí CRM
        /// </summary>
        private readonly string baseUrl;
        /// <summary>
        /// exportér výsledků importu
        /// </summary>
        public TxtExporter txtExporter;

        /// <summary>
        /// Kontruktor, v jehož těle dojde k přiřazení parametrů a vytvoření nové instance exportéru
        /// </summary>
        /// <param name="userName">viz výše</param>
        /// <param name="password">viz výše</param>
        /// <param name="parallelTasks">viz výše</param>
        /// <param name="baseUrl">viz výše</param>
        public CsvImporter(string userName, string password, int parallelTasks, string baseUrl)
        {
            this.userName = userName;
            this.password = password;
            this.parallelTasks = parallelTasks;
            this.baseUrl = baseUrl;
            txtExporter = new TxtExporter();
        }

        /// <summary>
        /// Hlavní metoda pro import souborů, od připojení k serveru s doklady, přes otevření zazipovaného souboru, 
        /// převodu ze CSV do CRM až po export výsledků
        /// </summary>
        /// <param name="url">adresa, kde se nalézá soubor s doklady</param>
        /// <param name="acmDocumentType">Typ dokladu</param>
        /// <param name="acmBatch">Zda obsahuje číslo série</param>
        public void Import(string url, int acmDocumentType, bool acmBatch)
        {
            dynamic lines = acmBatch ? new List<CsvLine>() : new List<CsvLineNoBatch>();
            HttpClient clientDownload = new HttpClient();
            try
            {
                using Stream stream = clientDownload.GetStreamAsync(url).Result;
                try
                {
                    Console.WriteLine("Import dat:\n\nPřipojuji se k serveru...");
                    lines = GetDataFromArchive(acmDocumentType, acmBatch, stream, lines);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            SendToCRM(lines, acmBatch);
            txtExporter.ExportResult(acmDocumentType, DateTime.Now);
        }

        /// <summary>
        /// Metoda, která otevře zazipovaný archiv se CSV souborem, který dále zpracuje
        /// </summary>
        /// <param name="acmDocumentType">typ dokladu</param>
        /// <param name="acmBatch">zda obsahuje číslo série</param>
        /// <param name="stream">Stream, přes který probíhá stahování souboru</param>
        /// <param name="lines">řádky CSV souboru s doklady, které se dále zpracovávají</param>
        /// <returns>zpracované řádky CSV souboru s doklady</returns>
        private static dynamic GetDataFromArchive(int acmDocumentType, bool acmBatch, Stream stream, dynamic lines)
        {
            using ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                Delimiter = ";",
            };

            try
            {
                Console.WriteLine("\nNačídám data z databáze...");
                using var sr = new StreamReader(archive.Entries[0].Open());
                using var csv = new CsvReader(sr, configuration);
                lines = acmBatch ? csv.GetRecords<CsvLine>().ToList() : csv.GetRecords<CsvLineNoBatch>().ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            foreach (var line in lines)
            {
                line.AcmDocumentType = acmDocumentType;
            }

            return lines;
        }

        /// <summary>
        /// Vyšle požadavky na server, nejprve v GET požadavku se ověří, jestli importovaný doklad již existuje v CRM tabulce a podle toho se odešle
        /// POST (vytvoření nového dokladu v tabulce) nebo PATCH požadavek (aktualizace již existujícího dokladu)
        /// </summary>
        /// <param name="lines">exportované řádky ze CSV souboru</param>
        /// <param name="acmBatch">zda doklad obsahuje číslo série</param>
        private void SendToCRM(dynamic lines, bool acmBatch)
        {
            HttpClientHandler clientHandler = new HttpClientHandler { Credentials = new NetworkCredential(userName, password) };
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            HttpClient client = new HttpClient(clientHandler);
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                Console.WriteLine("\nOdesílám požadavek k CRM...\n");
                var requestGet = Task.Run(() => client.GetAsync("acm_listinvaliddocuments")).Result;
                var responseTaskGet = requestGet.Content.ReadAsStringAsync().Result;
                var ResponseTaskJSON = JsonNode.Parse(responseTaskGet);
                using (SemaphoreSlim semaphore = new SemaphoreSlim(parallelTasks))
                {
                    List<Task> tasks = new List<Task>();
                    foreach (var line in lines)
                    {
                        semaphore.Wait();

                        if (ResponseTaskJSON["value"].AsArray().Where(x => x["acm_documentnumber"].ToString() == line.AcmDocumentNumber && x["acm_documenttype"].ToString() == line.AcmDocumentType.ToString() && x["acm_batch"]?.ToString() == line.AcmBatch).Count() > 0)
                        {
                            var task = Task.Run(async () =>
                            {
                                try
                                {
                                    string AcmId = ResponseTaskJSON["value"].AsArray().Where(x => x["acm_documentnumber"].ToString() == line.AcmDocumentNumber && x["acm_documenttype"].ToString() == line.AcmDocumentType.ToString() && x["acm_batch"]?.ToString() == line.AcmBatch).First()["acm_listinvaliddocumentid"].ToString();
                                    await UpdateRowCRM(client, line, AcmId);
                                }
                                finally
                                {
                                    semaphore?.Release();
                                }
                            });
                            tasks.Add(task);
                        }
                        else
                        {
                            var task = Task.Run(async () =>
                            {
                                try
                                {
                                    await CreateRowCRM(client, line);
                                }
                                finally
                                {
                                    semaphore.Release();
                                }
                            });
                            tasks.Add(task);
                        }
                    }
                    Task.WaitAll(tasks.ToArray());
                    txtExporter.SaveTasks(tasks.Count);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Post požadavek na vytvoření nového dokladu v CRM tabulce
        /// </summary>
        /// <param name="client">HTTP client, pomocí něhož se komunikuje se CRM</param>
        /// <param name="line">řádek ze CSV souboru s doklady</param>
        /// <returns>Vypíše do konzole zprávu z požadavku a také ji uloží do seznamu odpovědí ze serveru</returns>
        private async Task CreateRowCRM(HttpClient client, dynamic line)
        {
            try
            {
                var requestPost = new HttpRequestMessage(HttpMethod.Post, "acm_listinvaliddocuments");
                requestPost.Content = new StringContent(JsonSerializer.Serialize(line), encoding: Encoding.UTF8, "application/json");
                await client.SendAsync(requestPost).ContinueWith(async responseTaskPost =>
                {
                    Console.WriteLine(responseTaskPost.Result + "\n");
                    txtExporter.SaveResult(responseTaskPost.Result);
                    if (!responseTaskPost.Result.IsSuccessStatusCode || responseTaskPost.IsFaulted || responseTaskPost.IsCanceled || !responseTaskPost.IsCompletedSuccessfully || !responseTaskPost.IsCompleted)
                        await CreateRowCRM(client, line);
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Patch požadavek na aktualizaci již existujícího dokladu v CRM
        /// </summary>
        /// <param name="client">HTTP client, pomocí něhož se komunikuje se CRM</param>
        /// <param name="line">řádek ze CSV souboru s doklady</param>
        /// <param name="AcmId">primární klíč daného dokladu v CRM tabulce</param>
        /// <returns>Vypíše do konzole zprávu z požadavku a také ji uloží do seznamu odpovědí ze serveru</returns>
        private async Task UpdateRowCRM(HttpClient client, dynamic line, string AcmId)
        {
            try
            {
                var requestPatch = new HttpRequestMessage(HttpMethod.Patch, $"acm_listinvaliddocuments({AcmId})");
                requestPatch.Content = new StringContent(JsonSerializer.Serialize(line), encoding: Encoding.UTF8, "application/json");
                await client.SendAsync(requestPatch).ContinueWith(async responseTaskPatch =>
                {
                    Console.WriteLine(responseTaskPatch.Result + "\n");
                    txtExporter.SaveResult(responseTaskPatch.Result);
                    if (!responseTaskPatch.Result.IsSuccessStatusCode || responseTaskPatch.IsFaulted || responseTaskPatch.IsCanceled || !responseTaskPatch.IsCompletedSuccessfully || !responseTaskPatch.IsCompleted)
                        await UpdateRowCRM(client, line, AcmId);
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Metoda, která vymaže ze CRM tabulky doklady
        /// </summary>
        public void ClearCRM()
        {
            HttpClientHandler clientHandler = new HttpClientHandler { Credentials = new NetworkCredential(userName, password) };
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            HttpClient client = new HttpClient(clientHandler);
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            bool noResult = false;
            List<Task> tasks = new List<Task>();
            while (!noResult)
            {
                Console.WriteLine("Mazání dat v CRM:\n\nOdesílám požadavek k CRM...\n");
                var requestGet = Task.Run(() => client.GetAsync("acm_listinvaliddocuments"));
                var responseTaskGet = requestGet.Result.Content.ReadAsStringAsync().Result;
                var ResponseTaskJSON = JsonNode.Parse(responseTaskGet);

                if (ResponseTaskJSON["value"].AsArray().Count == 0)
                    noResult = true;

                using (SemaphoreSlim semaphore = new SemaphoreSlim(parallelTasks))
                {
                    for (int i = 0; i < ResponseTaskJSON["value"].AsArray().Count; i++)
                    {
                        semaphore.Wait();
                        string AcmId = ResponseTaskJSON["value"][i]["acm_listinvaliddocumentid"].ToString();
                        var task = Task.Run(async () =>
                        {
                            try
                            {
                                await DeleteRowCRM(client, AcmId);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        });

                        tasks.Add(task);
                    }

                    Task.WaitAll(tasks.ToArray());
                }
            }
        }

        /// <summary>
        /// Vymaže jeden doklad s určitým primárním klíčem z tabulky CRM 
        /// </summary>
        /// <param name="client">HTTP client, pomocí něhož se komunikuje se CRM</param>
        /// <param name="AcmId">Primární klíč dokladu v tabulce CRM</param>
        /// <returns></returns>
        private static async Task DeleteRowCRM(HttpClient client, string AcmId)
        {
            try
            {
                var requestDelete = new HttpRequestMessage(HttpMethod.Delete, $"acm_listinvaliddocuments({AcmId})");
                await client.SendAsync(requestDelete).ContinueWith(responseTaskDelete => { Console.WriteLine(responseTaskDelete.Result + "\n"); });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}