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

namespace Parser.Server.Code
{
    public class ParserService : BackgroundService
    {
        private readonly ILogger<ParserService> logger;

        public bool IsBusy { get; private set; }
        private bool NeedStop;
        //private readonly ParsingSettings _settings;

        //private readonly IEventBus _eventBus;

        public ParserService(IOptions<Settings> settings,
                                         //IEventBus eventBus,
                                         ILogger<ParserService> logger)
        {
            this.logger = logger;
            //Constructor’s parameters validations...
        }

        protected override async Task ExecuteAsync(CancellationToken parsingToken)
        {
            IsBusy = true;
            logger.LogDebug($"GracePeriodManagerService is starting.");
            return;
            var chromeOptions = new ChromeOptions();
            //chromeOptions.AddArgument("--blink-settings=imagesEnabled=false");
            //chromeOptions.AddArgument("--disable-application-cache");
            var browser = new ChromeDriver(Constants.WebDriverFolder, chromeOptions);



            

            parsingToken.Register(() =>
                logger.LogDebug($" GracePeriod background task is stopping."));

            while (!NeedStop && !parsingToken.IsCancellationRequested)
            {
                logger.LogDebug($"GracePeriod task doing background work.");



                browser.Navigate().GoToUrl(Constants.RusProfileUrl);


                IJavaScriptExecutor js = (IJavaScriptExecutor)browser;
                string valOut = js.ExecuteScript("var element = document.querySelector('div.main-section__title'); return element.innerHTML; ")?.ToString();




                // This eShopOnContainers method is querying a database table
                // and publishing events into the Event Bus (RabbitMQ / ServiceBus)
                //CheckConfirmedGracePeriodOrders();

                await Task.Delay(5000, parsingToken);
            }

            IsBusy = false;

            logger.LogDebug($"GracePeriod background task is stopping.");
        }

        public void Stop()
        {
            NeedStop = true;
            while (IsBusy) Thread.Sleep(1000);
        }

        public void Start()
        {
            NeedStop = false;
            StartAsync(new CancellationToken(false)).Wait();
            IsBusy = true;
        }
    }
}
