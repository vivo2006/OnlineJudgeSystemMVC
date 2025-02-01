using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer
{
    public class Judge0Result
    {
        public string? stdout { get; set; }
        public string? time { get; set; }
        public float? memory { get; set; }
        public Status status { get; set; }
    }
}
