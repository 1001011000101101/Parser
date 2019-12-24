using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Parser.Shared.Models;
using LiteDB;
using NLog.Web;
using System.Threading;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.IO;
using OfficeOpenXml;


namespace Parser.Server.Code
{
    public class BaseWorker
    {
        protected readonly ILogger<ParserService> logger;

        public ParserState State { get; private set; }
        
        protected bool needStop;

        protected Settings appSettings;
        protected List<Company> companies;
        protected IHostEnvironment env;

        public BaseWorker(IHostEnvironment env, ILogger<ParserService> logger)
        {
            State = new ParserState();
            this.logger = logger;
            this.env = env;

            appSettings = GetSettings();
            companies = GetCompanies(appSettings);
        }

        public virtual void DoWork()
        {
        }

        public void Stop()
        {
            needStop = true;
            while (State.IsBusy) Thread.Sleep(1000);
        }


        protected Settings GetSettings()
        {
            using (var db = new LiteDatabase(System.IO.Path.Combine(env.ContentRootPath, Constants.DbFileLocation)))
            {
                var settings = db.GetCollection<Settings>(nameof(Settings).ToLower());

                var s = settings.FindAll().SingleOrDefault();
                if (s == null)
                {
                    settings.Insert(new Settings());
                }

                return settings.FindAll().SingleOrDefault();
            }
        }

        protected List<Company> GetCompanies(Settings settings)
        {
            List<Company> c = new List<Company>();

            try
            {
                string path = Path.Combine(env.ContentRootPath, Constants.UploadFilesFolder, settings.CompaniesFileName);
                using (var fs = new FileStream(path, System.IO.FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var excelPackage = new ExcelPackage(fs))
                    {
                        var excelWorkbook = excelPackage.Workbook;
                        var sheet = excelWorkbook.Worksheets[1];

                        int colCount = sheet.Dimension.End.Column;  //get Column Count
                        int rowCount = sheet.Dimension.End.Row;     //get row count
                        for (int row = 1; row <= rowCount; row++)
                        {
                            if (sheet.Cells[row, 1].Value == null) continue;

                            c.Add(new Company()
                            {
                                Inn = sheet.Cells[row, 1].Value.ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
            }


            return c;
        }

        protected void UpdateCompanies(List<Company> companies)
        {


            try
            {
                using (var db = new LiteDatabase(Path.Combine(env.ContentRootPath, Constants.DbFileLocation)))
                {
                    // Get a collection (or create, if doesn't exist)
                    var col = db.GetCollection<Company>("Companies");

                    foreach (var c in companies)
                    {
                        var company = col.FindOne(x => x.Inn.Equals(c.Inn));


                        if (company == null)
                        {
                            col.Insert(c);
                        }
                        else
                        {
                            col.Update(company);
                        }


                        // Update a document inside a collection
                        //settings.Region = "Тюмень";

                        //col.Update(settings);

                        // Index document using document Name property
                        col.EnsureIndex(x => x.Inn);
                    }
                    

                    // Use LINQ to query documents




                    // Let's create an index in phone numbers (using expression). It's a multikey index
                    //col.EnsureIndex(x => x.Phones, "$.Phones[*]");

                    // and now we can query phones
                    //var r = col.FindOne(x => x.Phones.Contains("8888-5555"));
                }
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
            }
        }

    }
}
