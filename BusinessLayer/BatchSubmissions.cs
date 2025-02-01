using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer
{
    public class BatchSubmissions
    {
        public List<PostRequest> submissions = new List<PostRequest>();
        public BatchSubmissions()
        {
            submissions = new List<PostRequest>();
        }
    }
}
