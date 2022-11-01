using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Import_Invalid_Documents
{
    /// <summary>
    /// Třída, která má na starosti výstupy z importu dat
    /// </summary>
    public class TxtExporter
    {

        /// <summary>
        /// cesta k souboru s nastaveními
        /// </summary>
        private static readonly string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MS", "Import Invalid Documents");

        /// <summary>
        /// seznam odpovědí serveru, na něhož byly zaslány požadavky
        /// </summary>
        List<HttpResponseMessage> results;
        /// <summary>
        /// Počet všech úloh (požadavky na server), které byly zadané během importu
        /// </summary>
        private int countAllTasks;

        /// <summary>
        /// Konstruktor, který v těle vytváří novou instanci seznamu odpovědí ze serveru
        /// </summary>
        public TxtExporter()
        {
            results = new List<HttpResponseMessage>();
        }

        /// <summary>
        /// Přidá do seznamu odpovědí ze serveru odpověď z jednoho požadavku na server
        /// </summary>
        /// <param name="message">odpověď ze serveru</param>
        public void SaveResult(HttpResponseMessage message)
        {
            results.Add(message);
        }

        /// <summary>
        /// Po skončení importu dat se přiřadí počet provedenýh úloh (požadavků na server), úspěšně i neúspěšně vykonaných
        /// </summary>
        /// <param name="countAllTasks"></param>
        public void SaveTasks(int countAllTasks)
        {
            this.countAllTasks = countAllTasks;
        }

        /// <summary>
        /// Vyexportuje výsledky importů do dvou textových souborů, jeden s odpověďmi ze serveru a druhý, kolik se zvládlo naimportovat souboru z celkového počtu.
        /// </summary>
        /// <param name="acmDocumentType">typ dokumentu v CRM</param>
        /// <param name="date">dnešní datum</param>
        public void ExportResult(int acmDocumentType, DateTime date)
        {
            if (!Directory.Exists(Path.Combine(path, "Result of imports")))
            {
                Directory.CreateDirectory(Path.Combine(path, "Result of imports"));
            }

            if (!Directory.Exists(Path.Combine(path, "Reports of imports")))
            {
                Directory.CreateDirectory(Path.Combine(path, "Reports of imports"));
            }

            string dateFormatted = date.ToString("yyyyMMdd");
            using (StreamWriter writer = File.CreateText(Path.Combine(path, "Result of imports", dateFormatted + "_" + acmDocumentType.ToString() + ".txt")))
            {
                foreach (var result in results)
                {
                    writer.Write("Server: " + result.Headers.Location + "\nDate: " + result.Headers.Date + "\nStatusCode: " + (int)result.StatusCode + "\nReason Phrase: " + result.ReasonPhrase + "\n\n");
                }
            }

            using (StreamWriter writer = File.CreateText(Path.Combine(path, "Reports of imports", dateFormatted + "_" + acmDocumentType.ToString() + ".txt")))
            {
                writer.Write("Bylo úspěšně naimporotváno " + results.Where(x => x.IsSuccessStatusCode).Count() + " neplatných dokladů z " + countAllTasks + ".");
            }
            results.Clear();
        }
    }
}
