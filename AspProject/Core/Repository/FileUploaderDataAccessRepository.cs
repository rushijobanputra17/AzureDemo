using Core.Service;
using Core.ViewModel;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Configuration;

namespace Core.Repository
{
    public class FileUploaderDataAccessRepository : IFileUploaderInterfaceService
    {

        private static string connectionString = string.Empty;
        private readonly IConfiguration _configuration;

        public FileUploaderDataAccessRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            connectionString = _configuration.GetConnectionString("dbConnectionString");


        }

    
        public List<FileUploaderViewModel> GetAllFiles()
        {
            List<FileUploaderViewModel> files = new();
            JsonResponse response = new JsonResponse();
            try
            {
                using (IDbConnection dbConnection = new SqlConnection(connectionString))
                {
                    files = dbConnection.Query<FileUploaderViewModel>("GetAllFiles").ToList();
                }
                response.Status = 1;
            }
            catch (Exception ex)
            {
                files = new List<FileUploaderViewModel>();
            }
            return files;
        }

        public JsonResponse SaveFile(FileUploaderViewModel model)
        {
           
            JsonResponse response = new JsonResponse();
            try
            {
                using (IDbConnection dbConnection = new SqlConnection(connectionString))
                {
                    response.Status = dbConnection.Query<int>("SaveFileDetails @fileName,@fileType,@localFilePath,@fileDescription,@fileBytes", new {
                        fileName=model.FileName,
                        fileType=model.ContentType,
                        localFilePath=model.FilePath,
                        fileDescription=model.FileDescription,fileBytes = model.FileBytes }).FirstOrDefault();
                }
               
            }
            catch (Exception ex)
            {
                response.Status = -1;
                response.Message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
            }
            return response;
        }
    } 
}
