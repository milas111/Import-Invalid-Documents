using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Import_Invalid_Documents
{
    /// <summary>
    /// Statická třída spravující veškeré parametry pro import souborů, obsahuje metody, které načítají parametry z textového souboru
    /// </summary>
    public static class Settings
    {
        /// <summary>
        /// cesta k souboru s nastaveními
        /// </summary>
        private static readonly string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MS", "Import Invalid Documents");

        /// <summary>
        /// název textového souboru s nastaveními
        /// </summary>
        private static readonly string filename = "settings.txt";

        /// <summary>
        /// textový soubor ve formátu JSON
        /// </summary>
        private static readonly JsonNode settings = GetSettingParameters();

        /// <summary>
        /// Vrací uživatelské jméno pro přihlášení do CRM
        /// </summary>
        /// <returns>Uživatelské jméno</returns>
        public static string GetUserName()
        {
            return settings["Username"].ToString();
        }

        /// <summary>
        /// Vrací heslo pro přihlášení do CRM
        /// </summary>
        /// <returns>Heslo</returns>
        public static string GetPassword()
        {
            return settings["Password"].ToString();
        }

        /// <summary>
        /// Vrací maximální počet paralelních požadavků na server
        /// </summary>
        /// <returns>Počet simultánních požadavků na server</returns>
        public static int GetParralelTasks()
        {
            return int.Parse(settings["ParallerTasks"].ToString());
        }

        /// <summary>
        /// Vrací základní adresu API rozhraní CRM
        /// </summary>
        /// <returns>url adresu</returns>
        public static string GetBaseUrl()
        {
            return settings["BaseUrl"].ToString();
        }

        /// <summary>
        /// Vrací řetězec informací k importu dat, jako adresa importovaného souboru,
        /// jestli doklad obsahuje sériové číslo, k jakému typu dokladu se má v CRM přiřadit a jestli má importovat.
        /// </summary>
        /// <returns>řetězec informací</returns>
        public static JsonNode GetDownloadParameters()
        {
            return settings["DownloadParameters"];
        }

        /// <summary>
        /// Vrací jestli má dojít k importu všech souborů
        /// </summary>
        /// <returns>true nebo false</returns>
        public static bool Import()
        {
            return bool.Parse(settings["Import"].ToString());
        }

        /// <summary>
        /// Vrací, jestli má dojít k promázání tabulky neplatných dokladů
        /// </summary>
        /// <returns>true nebo false</returns>
        public static bool Delete()
        {
            return bool.Parse(settings["Delete"].ToString());
        }

        /// <summary>
        /// Vrací celý textový soubor ve formátu JSON, který se bude dále zpracovávat
        /// </summary>
        /// <param name="filename">cesta k textovému souboru</param>
        /// <returns>textový soubor v JSON</returns>
        private static JsonNode GetSettingParameters()
        {
            InicializeSettings();
            using (StreamReader reader = File.OpenText(Path.Combine(path, filename)))
            {
                JsonNode settings = JsonNode.Parse(reader.ReadToEnd().Replace(@"\", @"\\"));

                return settings;
            }
        }

        /// <summary>
        /// Vytvoří textový soubor s potřebnými parametry, pokud neexistuje
        /// </summary>
        /// <param name="filename">cesta k textovému souboru</param>
        public static void InicializeSettings()
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            if (!File.Exists(Path.Combine(path, filename)))
            {
                File.Create(Path.Combine(path, filename)).Close();
                using (StreamWriter writer = File.CreateText(Path.Combine(path, filename)))
                {
                    writer.WriteLine("{");
                    writer.WriteLine(@"""Username"":""ACMARK\soukup"",");
                    writer.WriteLine(@"""Password"":""ghc1w70KFgr2"",");
                    writer.WriteLine(@"""ParallerTasks"":""100"",");
                    writer.WriteLine(@"""BaseUrl"":""https://dev.acmark.eu:5555/ACMARK/api/data/v8.2/"",");
                    writer.WriteLine(@"""Delete"":false,");
                    writer.WriteLine(@"""Import"":true,");
                    writer.WriteLine(@"""DownloadParameters"":[");
                    writer.WriteLine(@"{""url"":""https://aplikace.mvcr.cz/neplatne-doklady/ViewFile.aspx?typ_dokladu=0"",""AcmType"":805210000,""AcmBatch"":false,""Import"":true},");
                    writer.WriteLine(@"{""url"":""https://aplikace.mvcr.cz/neplatne-doklady/ViewFile.aspx?typ_dokladu=1"",""AcmType"":805210001,""AcmBatch"":true,""Import"":true},");
                    writer.WriteLine(@"{""url"":""https://aplikace.mvcr.cz/neplatne-doklady/ViewFile.aspx?typ_dokladu=0"",""AcmType"":805210002,""AcmBatch"":false,""Import"":true}");
                    writer.WriteLine("]");
                    writer.WriteLine("}");
                }
            }
        }
    }
}