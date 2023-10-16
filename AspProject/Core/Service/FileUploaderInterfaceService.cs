using Core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service
{
    public interface IFileUploaderInterfaceService
    {
        List<FileUploaderViewModel> GetAllFiles();
        JsonResponse SaveFile(FileUploaderViewModel model);
    }
}
