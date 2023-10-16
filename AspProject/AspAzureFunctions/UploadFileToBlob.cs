using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Core.ViewModel;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using Dapper;
using System.Linq;
using static System.Net.WebRequestMethods;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;

namespace AspAzureFunctions
{
   
    public static class UploadFileToBlob
    {

        
        private static readonly string localdbConncectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
        private static readonly string azureStorageConnectionString = Environment.GetEnvironmentVariable("AzureConnectionString");
        private static readonly string azureContainer = Environment.GetEnvironmentVariable("azureContainer");
        [FunctionName("UploadFileToBlob")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string responseMessage = null;
            try {
                int fileId = 0;

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                fileId = data !=null ? Convert.ToInt32(data) :0;

                 responseMessage = fileId == 0
                    ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                    : $"fileid : , {fileId}. This HTTP triggered function executed successfully.";

                log.LogInformation(" fileId :" + fileId);

                if (fileId > 0)
                {

                    var list = GetAllFiles(log, fileId);
                    if (list != null && list.Any())
                    {
                        var fileObj = list.FirstOrDefault();
                        uploadFileToBlobStorage(fileObj, log);
                    }
                    else
                    {
                        log.LogInformation("code run for azuee");
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogInformation("function error"+ex.InnerException == null ? ex.Message : ex.InnerException.Message);
            }
            log.LogInformation("C# HTTP trigger function processed a request.");

            //string name = req.Query["name"];
         


           
            return new OkObjectResult(responseMessage);
        }


        private static CloudBlobContainer GetAzureContainerRef(ILogger log)
        {
            log.LogInformation($"GetAzureContainerRef START: {DateTime.UtcNow}");
            try
            {

                CloudStorageAccount account = CloudStorageAccount.Parse(azureStorageConnectionString);
                CloudBlobClient client = account.CreateCloudBlobClient();
                CloudBlobContainer cblob = client.GetContainerReference(azureContainer);
                cblob.CreateIfNotExistsAsync();
                if (cblob != null)
                {
                    cblob.SetPermissionsAsync(
                        new BlobContainerPermissions
                        {
                            PublicAccess = BlobContainerPublicAccessType.Blob
                        }
                    );
                }
                return cblob;
            }
            catch (Exception ex)
            {
                string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                log.LogInformation($"GetAzureContainerRef error: {message}");
            }
            return null;
        }


        private static void UpdateBackUpURL(int fileId, string backUpURL, ILogger log)
        {
            try
            {
                using (IDbConnection dbConnection = new SqlConnection(localdbConncectionString))
                {
                    dbConnection.Query("UpdateBackUpUrl @fileId,@backUpURL", new
                    {
                        fileId = fileId,
                        backUpURL = backUpURL
                    });
                }
            }
            catch (Exception ex)
            {
                log.LogInformation("UpdateBackUpURL err" + ex.InnerException == null ? ex.Message : ex.InnerException.Message);

            }

        }

        public static void uploadFileToBlobStorage(FileUploaderViewModel fileObj, ILogger log)
        {
            log.LogInformation("uploadFileToBlobStorage Start");
            try
            {
                var azureContainer = GetAzureContainerRef(log);
                if (!string.IsNullOrEmpty(fileObj.FileBytes))
                {
                    var fileBytes = Convert.FromBase64String(fileObj.FileBytes);
                    //var fileContent = new StreamContent(new MemoryStream(fileBytes));
                    CloudBlockBlob cloudBlob = azureContainer.GetBlockBlobReference(fileObj.FileName);
                    using (var s = new MemoryStream(fileBytes))
                    {
                        cloudBlob.UploadFromStreamAsync(s).Wait();
                        string fileUrl = cloudBlob.Uri.AbsoluteUri;
                        if (!string.IsNullOrEmpty(fileUrl))
                        {
                            UpdateBackUpURL(fileObj.FileUploaderId, fileUrl, log);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogInformation("uploadFileToBlobStorage error : "+ex.InnerException==null ? ex.Message:ex.InnerException.Message);
            }

            log.LogInformation("uploadFileToBlobStorage END");
        }

        public static List<FileUploaderViewModel> GetAllFiles(ILogger log,int fileId=0)
        {
            log.LogInformation("GetAllFiles with Id:" + fileId);
            List<FileUploaderViewModel> files = new List<FileUploaderViewModel>();
            JsonResponse response = new JsonResponse();
            try
            {
                using (IDbConnection dbConnection = new SqlConnection(localdbConncectionString))
                {
                    files = dbConnection.Query<FileUploaderViewModel>("GetAllFiles @fileId", new { fileId = fileId }).ToList();
                }
                response.Status = 1;
                if (files!=null && files.Any())
                {
                    log.LogInformation("file received :" + JsonConvert.SerializeObject(files));
                }
                else
                {
                    log.LogInformation("file received : no file found");
                }
                log.LogInformation("fun rus run");
                
            }
            catch (Exception ex)
            {
                log.LogInformation("file received in catch: no file found");
                log.LogInformation("GetAllFiles error : "+ex.InnerException==null?ex.Message:ex.InnerException.Message);
                files = new List<FileUploaderViewModel>();

            }
            return files;
        }

    }
}
