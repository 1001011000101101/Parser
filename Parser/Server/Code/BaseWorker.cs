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
        protected IDb db;

        public BaseWorker(IHostEnvironment env, ILogger<ParserService> logger, IDb db)
        {
            State = new ParserState();
            this.logger = logger;
            this.env = env;
            this.db = db;

            appSettings = db.GetSettings(env.ContentRootPath);
            companies = db.GetCompaniesFromExcel(env.ContentRootPath);
        }

        public virtual void DoWork()
        {
        }

        public void Stop()
        {
            needStop = true;
            while (State.IsBusy) Thread.Sleep(1000);
        }
    }
}
