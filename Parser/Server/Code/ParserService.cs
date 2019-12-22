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
    public class ParserService : BackgroundService
    {
        private readonly ILogger<ParserService> logger;

        public ParserState State { get; private set; }
        private bool needStop;
        private IHostEnvironment env;
        private Settings appSettings;
        private List<Company> companies;
        //private readonly ParsingSettings _settings;

        //private readonly IEventBus _eventBus;

        public ParserService(IHostEnvironment env, ILogger<ParserService> logger)
        {
            this.logger = logger;
            this.env = env;
            State = new ParserState();
        }

        protected override async Task ExecuteAsync(CancellationToken parsingToken)
        {
            if (!State.IsBusy)
                return;

            try
            {
                logger.LogDebug($"ParserService is starting.");
                appSettings = GetSettings();
                companies = GetCompanies(appSettings);



                var chromeOptions = new ChromeOptions();
                //chromeOptions.AddArguments("headless");
                //chromeOptions.AddArgument("--blink-settings=imagesEnabled=false");
                chromeOptions.AddArgument("--disable-application-cache");
                var browser = new ChromeDriver(Constants.WebDriverFolder, chromeOptions);
                IJavaScriptExecutor js = (IJavaScriptExecutor)browser;

                //parsingToken.Register(() =>
                //    logger.LogDebug($" GracePeriod background task is stopping."));

                for (int i = 0; i < companies.Count; i++)
                {
                    var company = companies[i];
                    State.Description = $"Work with {company.Inn} ({i}/{companies.Count}). Get debt-to-income ratio";


                    browser.Navigate().GoToUrl(Constants.RusProfileUrl);

                    var element = browser.FindElement(By.CssSelector("#indexsearchform input.index-search-input"));
                    element.Clear();
                    element.SendKeys(company.Inn);

                    js.ExecuteScript($"document.querySelector('#indexsearchform button.search-btn').click();");

                    company.Ogrn = js.ExecuteScript("var element = document.querySelector('#clip_ogrn'); return element.innerHTML; ")?.ToString();

                    browser.Navigate().GoToUrl(Constants.RusProfileFinReportsUrl + company.Ogrn);

                    var isFinReportsExists = js.ExecuteScript("var element = document.querySelector('body.page404'); return element == null; ")?.ToString();

                    var rtrtrt = "";
                    await Task.Delay(5000, parsingToken);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
            }

            


            State.IsBusy = false;
            //return;
            

            //parsingToken.Register(() =>
            //    logger.LogDebug($" GracePeriod background task is stopping."));

            //while (!needStop && !parsingToken.IsCancellationRequested)
            //{
            //    logger.LogDebug($"GracePeriod task doing background work.");



            //    browser.Navigate().GoToUrl(Constants.RusProfileUrl);


                
            //    string valOut = js.ExecuteScript("var element = document.querySelector('div.main-section__title'); return element.innerHTML; ")?.ToString();




            //    // This eShopOnContainers method is querying a database table
            //    // and publishing events into the Event Bus (RabbitMQ / ServiceBus)
            //    //CheckConfirmedGracePeriodOrders();

            //    await Task.Delay(5000, parsingToken);
            //}

            

            logger.LogDebug($"GracePeriod background task is stopping.");
        }

        public void Stop()
        {
            needStop = true;
            while (State.IsBusy) Thread.Sleep(1000);
        }

        public void Start()
        {
            needStop = false;
            State.IsBusy = true;
            StartAsync(new CancellationToken(false)).Wait();
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
    }
}
