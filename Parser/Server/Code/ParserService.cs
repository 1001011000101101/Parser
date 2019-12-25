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
        protected readonly ILogger<ParserService> logger;

        public ParserState State { get; private set; }

        public ParserState ParserByInnState 
        { 
            get 
            {
                if (parserByInnWorker == null)
                    parserByInnWorker = new ParserByInnWorker(env, logger, db);

                return parserByInnWorker.State;
            } 
            private set { } 
        }

        private ParserByInnWorker parserByInnWorker;

        private bool needStop;
        protected IHostEnvironment env;
        protected IDb db;


        public ParserService(IHostEnvironment env, ILogger<ParserService> logger, IDb db)
        {
            this.logger = logger;
            this.env = env;
            State = new ParserState();
            this.db = db;
        }

        protected override async Task ExecuteAsync(CancellationToken parsingToken)
        {
            try
            {
                logger.LogDebug($"ParserService is starting.");

                Proxy.RefreshList();

                parserByInnWorker = new ParserByInnWorker(env, logger, db);


                parsingToken.Register(() =>
                    logger.LogDebug($" ParserService background task is stopping."));

                


                while (!needStop && !parsingToken.IsCancellationRequested)
                {
                    logger.LogDebug($"ParserService task doing background work.");
                    await Task.Delay(5000, parsingToken);
                }

            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
            }

            State.IsBusy = false;
            logger.LogDebug($"ParserService background task is stopping.");
        }

        //public void Stop()
        //{
        //    needStop = true;
        //    while (State.IsBusy) Thread.Sleep(1000);
        //}

        //public void Start()
        //{
        //    needStop = false;
        //    State.IsBusy = true;
        //    StartAsync(new CancellationToken(false)).Wait();
        //}

        public void StartParserByInn()
        {
            Task.Factory.StartNew(() => parserByInnWorker.DoWork());
        }

        public void StopParserByInn()
        {
            parserByInnWorker.Stop();
        }

    }
}
