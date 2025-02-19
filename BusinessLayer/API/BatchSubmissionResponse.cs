using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BusinessLayer.API
{
    public class BatchSubmissionResponse
    {
        [JsonPropertyName("submissions")]
        public List<Judge0Result> Submissions { get; set; }
    }
}
