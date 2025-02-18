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
        public int language_id { get; set; }
        [JsonPropertyName("source_code")]
        public string source_code { get; set; }
        [JsonPropertyName("stdin")]
        public string stdin { get; set; }
        [JsonPropertyName("expected_output")]
        public string expected_output { get; set; }
        [JsonPropertyName("cpu_time_limit")]
        public double cpu_time_limit { get; set; }
        [JsonPropertyName("memory_limit")]
        public double memory_limit { get; set; }
    }
}
