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

        public ParserInfo ParserInfo { get; private set; }
        
        protected bool needStop;

        protected Settings appSettings;
        protected List<Company> companies;
        protected IHostEnvironment env;
        protected IDb db;
        protected string proxyAccessCode = string.Empty;

        public BaseWorker(IHostEnvironment env, ILogger<ParserService> logger, IDb db, string proxyAccessCode)
        {
            ParserInfo = new ParserInfo() { State = (int)Enums.ParserState.Stopped };
            this.logger = logger;
            this.env = env;
            this.db = db;
            



            this.proxyAccessCode = proxyAccessCode;
        }

        public virtual void DoWork()
        {
        }

        public void Stop()
        {
            needStop = true;
            while (ParserInfo.State == (int)Enums.ParserState.Started) Thread.Sleep(1000);
        }
    }
}
