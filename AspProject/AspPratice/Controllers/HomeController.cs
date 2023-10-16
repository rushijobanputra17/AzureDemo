using AspPratice.Models;
using Core.Repository;
using Core.Service;
using Core.ViewModel;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using System.Configuration;

namespace AspPratice.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IFileUploaderInterfaceService fileUploaderInterfaceService;
        private string azureHttpTriggerUrl = null;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            fileUploaderInterfaceService = new FileUploaderDataAccessRepository(configuration);
            azureHttpTriggerUrl = configuration.GetConnectionString("AzureHttpTriggerPath"); ;



        }

        public IActionResult Index()
        {
            ViewBag.AzureHttpTriggerUrl = azureHttpTriggerUrl;
            var files = fileUploaderInterfaceService.GetAllFiles();
            return View(files);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public IActionResult SaveFiles(IFormFile fileDetail, string description = null)
        {
            string fileName = null;
            JsonResponse res = new JsonResponse();

            FileUploaderViewModel model = new FileUploaderViewModel();
            try
            {
                if (fileDetail != null)
                {
                    fileName = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss") + "__" + fileDetail.FileName;
                    model.FileDescription = description;
                    model.FileName = fileName;
                    model.ContentType = fileDetail.ContentType;

                    using (var ms = new MemoryStream())
                    {
                        fileDetail.CopyTo(ms);
                        var fileBytes = ms.ToArray();
                        model.FileBytes = Convert.ToBase64String(fileBytes);

                        model.FilePath = fileName;
                    }

                    res = fileUploaderInterfaceService.SaveFile(model);
                }
            }
            catch (Exception ex)
            {
                res.Status = -1;
                res.Message = ex.Message;
            }
           
            return Json(res);
        }

    }
}