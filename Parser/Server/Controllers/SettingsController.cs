using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Parser.Shared.Models;
using LiteDB;
using System.Web;
using Microsoft.Extensions.Hosting;
using NLog.Web;
using Parser.Server.Code;
using System.IO;
using System.Net.Http;
using OfficeOpenXml;


//using Newtonsoft.Json;



namespace Parser.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly ILogger<SettingsController> logger;
        private IHostEnvironment env;
        private ParserService parserService;

        public SettingsController(ILogger<SettingsController> logger, IHostEnvironment env, IHostedService parserService)
        {
            this.logger = logger;
            this.env = env;
            this.parserService = (ParserService)parserService;
        }

        [HttpGet]
        public Settings Get()
        {

            logger.LogDebug($"Get settings() root: {env.ContentRootPath} dbLocation: {Constants.DbFileLocation}");
            using (var db = new LiteDatabase(Path.Combine(env.ContentRootPath, Constants.DbFileLocation)))
            {
                // Get a collection (or create, if doesn't exist)
                var settings = db.GetCollection<Settings>(nameof(Settings).ToLower());
                // Use LINQ to query documents


                var s = settings.FindAll().SingleOrDefault();
                if (s == null)
                {
                    settings.Insert(new Settings());
                }

                return settings.FindAll().SingleOrDefault();
            }
        }

        [HttpPost]
        public Responce SaveSettingsAsync(Settings settings)
        {
            Responce result = new Responce();

            
     
            // Open database (or create if doesn't exist)
            using (var db = new LiteDatabase(System.IO.Path.Combine(env.ContentRootPath, Constants.DbFileLocation)))
            {
                // Get a collection (or create, if doesn't exist)
                var col = db.GetCollection<Settings>(nameof(Settings).ToLower());


                var results = col.FindById(settings.Id);


                if (results == null)
                {
                    col.Insert(settings);
                }
                else
                {
                    col.Update(settings);
                }
                

                // Update a document inside a collection
                //settings.Region = "Тюмень";

                //col.Update(settings);

                // Index document using document Name property
                col.EnsureIndex(x => x.Region);
                col.EnsureIndex(x => x.Okved);
                col.EnsureIndex(x => x.DebtPercent);

                // Use LINQ to query documents




                // Let's create an index in phone numbers (using expression). It's a multikey index
                //col.EnsureIndex(x => x.Phones, "$.Phones[*]");

                // and now we can query phones
                //var r = col.FindOne(x => x.Phones.Contains("8888-5555"));
            }




            result.Success = true;
            result.Message = "Настройки успешно сохранены";
            result.NeedShowMessage = true;



            return result;
        }

        [HttpGet]
        [Route("CompaniesInfo")]
        public Responce CompaniesInfo()
        {
            //logger.LogDebug("Get settings()");
            using (var db = new LiteDatabase(System.IO.Path.Combine(env.ContentRootPath, Constants.DbFileLocation)))
            {
                // Get a collection (or create, if doesn't exist)
                var settings = db.GetCollection<Settings>(nameof(Settings).ToLower()).FindAll().SingleOrDefault();
                var companies = db.GetCollection<Company>("Companies");

                return new CompaniesResponce()
                {
                    CompaniesFileIsUploaded = settings.CompaniesFileIsUploaded,
                    CompaniesCount = companies.Count(),
                    CompaniesFileName = settings.CompaniesFileName, 
                    CompaniesFileUploadedDate = settings.CompaniesFileUploadedDate
                };
            }
        }

        [HttpPost]
        [Route("UploadCompanies")]
        public async Task<HttpResponseMessage> UploadCompanies()
        {
            var result = new HttpResponseMessage();
            string fileName = string.Empty;
            string path = string.Empty;

            try
            {
                if (HttpContext.Request.Form.Files.Any())
                {
                    foreach (var file in HttpContext.Request.Form.Files)
                    {
                        fileName = file.FileName;
                        path = Path.Combine(env.ContentRootPath, Constants.UploadFilesFolder, file.FileName);
                        using (var stream = new FileStream(path, System.IO.FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                    }


                    // Open database (or create if doesn't exist)
                    using (var db = new LiteDatabase(Path.Combine(env.ContentRootPath, Constants.DbFileLocation)))
                    {
                        // Get a collection (or create, if doesn't exist)
                        var settings = db.GetCollection<Settings>(nameof(Settings).ToLower());

                        var s = settings.FindAll().SingleOrDefault();
                        if (s == null)
                        {
                            settings.Insert(new Settings());
                        }

                        //Delete previous uploaded file
                        string prevFile = Path.Combine(Path.GetDirectoryName(path), s.CompaniesFileName);
                        if (s.CompaniesFileIsUploaded && System.IO.File.Exists(prevFile))
                        {
                            try
                            {
                                System.IO.File.Delete(prevFile);
                            }
                            catch (Exception) {}
                        }

                        s.CompaniesFileIsUploaded = true;
                        s.CompaniesFileName = fileName;
                        s.CompaniesFileUploadedDate = DateTime.Now.Date;
                        settings.Update(s);

                        settings.EnsureIndex(x => x.CompaniesFileName);
                        settings.EnsureIndex(x => x.CompaniesFileIsUploaded);
                    }
                }
                result.StatusCode = System.Net.HttpStatusCode.OK;
                result.ReasonPhrase = "Компании успешно загружены";
            }
            catch (Exception e)
            {
                result.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                result.ReasonPhrase = e.Message;
                logger.LogDebug(e.Message);
            }


            return result;
        }

        [Route("DownloadCompanies")]
        public FileResult DownloadCompanies()
        {
            Settings settings = null;
            List<Company> companies = new List<Company>();

            using (var db = new LiteDatabase(Path.Combine(env.ContentRootPath, Constants.DbFileLocation)))
            {
                var collection = db.GetCollection<Settings>(nameof(Settings).ToLower());

                var s = collection.FindAll().SingleOrDefault();
                if (s == null)
                {
                    collection.Insert(new Settings());
                }

                settings = collection.FindAll().SingleOrDefault();

                companies = db.GetCollection<Company>("companies").FindAll().ToList();
            }
            string path = Path.Combine(env.ContentRootPath, Constants.UploadFilesFolder, settings.CompaniesFileName);

            string pathTemp = Path.Combine(env.ContentRootPath, Constants.TempFilesFolder, settings.CompaniesFileName);
            System.IO.File.Copy(path, pathTemp, true);

            using (ExcelPackage excelPackage = new ExcelPackage(new FileInfo(pathTemp)))
            {
                var excelWorkbook = excelPackage.Workbook;
                var sheet = excelWorkbook.Worksheets.Add(settings.CompaniesFileName);

                for (int row = 0; row < companies.Count; row++)
                {
                    sheet.Cells[row + 1, 1].Value = companies[row].Inn;
                    sheet.Cells[row + 1, 2].Value = companies[row].Ogrn;
                }

                excelPackage.Save();
            }


            return this.File(System.IO.File.ReadAllBytes(pathTemp), "application/zip", settings.CompaniesFileName);
        }
    }
}