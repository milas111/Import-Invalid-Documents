using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Import_Invalid_Documents
{
    /// <summary>
    /// Reprezentuje jeden řádek v CSV souboru dokladů, které neobsahují sériové číslo
    /// </summary>
    public class CsvLineNoBatch
    {
        /// <summary>
        /// Číslo dokladu
        /// </summary>
        [Index(0)]
        [JsonPropertyName("acm_documentnumber")]
        public string AcmDocumentNumber { get; set; }

        /// <summary>
        /// Datum zneplatnění dokladu
        /// </summary>
        [Index(1)]
        [JsonPropertyName("acm_invalidationdate")]
        [Format("d.m.yyyy")]
        public DateTimeOffset? AcmInvalidationDate { get; set; }

        /// <summary>
        /// Číslo série, které bude mít hodnotu null (pro zjednodušení práce s kódem je zde uvedeno)
        /// </summary>
        [Ignore]
        [JsonPropertyName("acm_batch")]
        public string? AcmBatch { get; set; }

        /// <summary>
        /// Typ dokumentu, který se přířadí později po převedení ze CSV souboru
        /// </summary>
        [Ignore]
        [JsonPropertyName("acm_documenttype")]
        public int AcmDocumentType { get; set; }
    }
}
