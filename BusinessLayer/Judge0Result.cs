using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BusinessLayer
{
    public class Judge0Result
    {
        [JsonPropertyName("stdout")]
        public string? Stdout { get; set; }
        [JsonPropertyName("time")]
        public string? Time { get; set; }
        [JsonPropertyName("memory")]
        public float? Memory { get; set; }
        [JsonPropertyName("status")]
        public Status Status { get; set; }
    }
}
