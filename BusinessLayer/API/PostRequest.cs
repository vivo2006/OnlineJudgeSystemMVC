using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BusinessLayer.API
{
    public class PostRequest
    {
        public int language_id { get; set; }
        public string source_code { get; set; }
        public string stdin { get; set; }
        public string expected_output { get; set; }
        public double cpu_time_limit { get; set; }
        public double memory_limit { get; set; }
    }
}
