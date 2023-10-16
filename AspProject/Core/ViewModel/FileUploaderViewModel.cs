using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.ViewModel
{
    public class FileUploaderViewModel
    {
        public int FileUploaderId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }

        public string BackUpFilePath { get; set; }

        public string FileDescription { get; set; } 

        public string ContentType { get; set; }

        public string FileBytes { get; set; }


    }
}
