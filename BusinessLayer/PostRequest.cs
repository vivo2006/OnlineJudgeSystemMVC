using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BusinessLayer
{
    public class PostRequest
    {
        [JsonPropertyName("language_id")]
        public int LanguageId { get; set; }
        [JsonPropertyName("source_code")]
        public string SourceCode { get; set; }
        [JsonPropertyName("stdin")]
        public string Stdin { get; set; }
        [JsonPropertyName("expected_output")]
        public string ExpectedOutput { get; set; }
        [JsonPropertyName("cpu_time_limit")]
        public double CpuTimeLimit { get; set; }
        [JsonPropertyName("memory_limit")]
        public double MemoryLimit { get; set; }
    }
}
