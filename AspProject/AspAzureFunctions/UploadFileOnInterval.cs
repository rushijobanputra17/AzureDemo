using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Reflection;
using Core.ViewModel;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapper;
using System.Linq;
using System.Drawing.Printing;
using Azure.Storage.Blobs;
using System.IO;
using System.Net.Http;
using Microsoft.Extensions.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using static System.Net.WebRequestMethods;

namespace AspAzureFunctions
{
    public class UploadFileOnInterval
    {
        private static readonly string localdbConncectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
        private static readonly string azureStorageConnectionString = Environment.GetEnvironmentVariable("AzureConnectionString");
        private static readonly string azureContainer = Environment.GetEnvironmentVariable("azureContainer");
        [FunctionName("UploadFileOnInterval")]
        public void Run([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer, ILogger log)
        {
            DateTime startDate = DateTime.UtcNow.AddDays(-1);
            DateTime endDate = DateTime.UtcNow;
            List<FileUploaderViewModel> files = new List<FileUploaderViewModel>();
            try
            {
                log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
                //string srcTable = Environment.GetEnvironmentVariable("localDbConnectionString");
               
                files = GetFileDetailForBackUp(startDate,endDate,log);
               
                if (files!=null && files.Any())
                {
                    foreach(FileUploaderViewModel file in files )
                    {
                        log.LogInformation($"file name: {file.FileName}");
                    }
                }
                UpdateFileToAzureBlobStorage(files,log);
                log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

                
            }
            catch (Exception ex)
            {
                log.LogInformation($" UploadFileOnInterval error: {ex.Message}");
            }


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
                string message = ex.InnerException==null ? ex.Message :ex.InnerException.Message;
                log.LogInformation($"GetAzureContainerRef error: {message}");
            }
            return null;
        }


        private static void UpdateFileToAzureBlobStorage(List<FileUploaderViewModel> files, ILogger log)
        {
            log.LogInformation($"UpdateFileToAzureBlobStorage START: {DateTime.Now}");
            try
            {
                var azureContainer = GetAzureContainerRef(log);
                foreach (var file in files)
                {
                    if (!string.IsNullOrEmpty(file.FileBytes))
                    {
                        var fileBytes = Convert.FromBase64String(file.FileBytes);
                        //var fileContent = new StreamContent(new MemoryStream(fileBytes));
                        CloudBlockBlob cloudBlob = azureContainer.GetBlockBlobReference(file.FileName);
                        using (var s = new MemoryStream(fileBytes))
                        {
                            cloudBlob.UploadFromStreamAsync(s).Wait();
                            string fileUrl = cloudBlob.Uri.AbsoluteUri;
                            if (!string.IsNullOrEmpty(fileUrl))
                            {
                                UpdateBackUpURL(file.FileUploaderId, fileUrl,log);
                            }
                        }
                    }
                   
                }

            }
            catch (Exception ex)
            {
                string message = ex.InnerException==null ? ex.Message : ex.InnerException.Message .ToString();
                log.LogInformation($"Error-UpdateFileToAzureBlobStorage : {message}");
            }
            log.LogInformation($"UpdateFileToAzureBlobStorage END: {DateTime.Now}");
        }


        private static void UpdateBackUpURL(int fileId, string backUpURL,ILogger log)
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
    

        private static List<FileUploaderViewModel> GetFileDetailForBackUp(DateTime startDate, DateTime endDate, ILogger log)
        {
            List<FileUploaderViewModel> files = new List<FileUploaderViewModel>();
            JsonResponse response = new JsonResponse();
            try
            {
                using (IDbConnection dbConnection = new SqlConnection(localdbConncectionString))
                {
                    files = dbConnection.Query<FileUploaderViewModel>("GetAzureFileDateWise @startDate,@endDate", new
                    {
                        @startDate = startDate,
                        @endDate = endDate
                    }).ToList();
                }



            }
            catch (Exception ex)
            {
                string message  = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                log.LogInformation($"GetFileDetailForBackUp error: {message}");
                files = new List<FileUploaderViewModel>();
            }
            return files;
        }

    }


}
