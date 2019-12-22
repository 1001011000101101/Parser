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
    public class DebtIncomeParserService : BackgroundService
    {
        private readonly ILogger<DebtIncomeParserService> logger;

        public bool IsBusy { get; private set; }
        private bool NeedStop;
        //private readonly ParsingSettings _settings;

        //private readonly IEventBus _eventBus;

        public DebtIncomeParserService(ILogger<DebtIncomeParserService> logger)
        {
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken parsingToken)
        {
            if (!IsBusy)
                return;

            logger.LogDebug($"DebtIncomeParserService is starting.");

            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("--blink-settings=imagesEnabled=false");
            chromeOptions.AddArgument("--disable-application-cache");
            chromeOptions.AddArguments("headless");
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
            IsBusy = true;
            StartAsync(new CancellationToken(false)).Wait();
        }
    }
}
