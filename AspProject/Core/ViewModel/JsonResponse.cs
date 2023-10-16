using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.ViewModel
{
    public class JsonResponse
    {
        public int Status { get; set; }

        public string Message { get; set; } 

        public dynamic Data { get; set; }   
        public string ExtraProperty { get; set; }   
    }
}
