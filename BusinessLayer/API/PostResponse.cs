using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BusinessLayer.API
{
    public class PostResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }
    }
}
