using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Import_Invalid_Documents
{
    public class CsvLine
    {
        /// <summary>
        /// Číslo dokladu
        /// </summary>
        [Index(0)]
        [JsonPropertyName("acm_documentnumber")]
        public string AcmDocumentNumber { get; set; }

        /// <summary>
        /// Číslo série
        /// </summary>
        [Index(1)]
        [JsonPropertyName("acm_batch")]
        public string AcmBatch { get; set; }

        /// <summary>
        /// Datum zneplatnění dokladu
        /// </summary>
        [Index(2)]
        [JsonPropertyName("acm_invalidationdate")]
        [Format("d.m.yyyy")]
        public DateTimeOffset? AcmInvalidationDate { get; set; }

        /// <summary>
        /// Typ dokumentu, který se přířadí později po převedení ze CSV souboru
        /// </summary>
        [Ignore]
        [JsonPropertyName("acm_documenttype")]
        public int AcmDocumentType { get; set; }
    }
}
