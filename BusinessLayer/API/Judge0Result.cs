using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BusinessLayer.API
{
    public class Judge0Result
    {
        //The otutput of the program
        [JsonPropertyName("stdout")]
        public string? Stdout { get; set; }
        //The time taken from the program
        [JsonPropertyName("time")]
        public string? Time { get; set; }
        //The memory used by the program
        [JsonPropertyName("memory")]
        public float? Memory { get; set; }
        //The error message if any
        [JsonPropertyName("status")]
        public Status Status { get; set; }
    }
}
