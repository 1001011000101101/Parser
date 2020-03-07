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


//using Newtonsoft.Json;



namespace Parser.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ParsingController : ControllerBase
    {
        private readonly ILogger<SettingsController> logger;
        private IHostEnvironment env;
        private ParserService parserService;

        public ParsingController(ILogger<SettingsController> logger, IHostEnvironment env, IHostedService parserService)
        {
            this.logger = logger;
            this.env = env;
            this.parserService = (ParserService)parserService;
        }

        [HttpGet]
        [Route("ParserByInnState")]
        public Responce Get()
        {
            ParserResponce result = new ParserResponce();
            result.Success = true;
            result.ParserInfo.State = parserService.ParserByInnState.State;
            result.ParserInfo.StateDescription = parserService.ParserByInnState.StateDescription;

            return result;
        }

        //[HttpPost]
        //public Responce StartParsing()
        //{
        //    Responce result = new Responce();

        //    parserService.Start();

        //    result.Success = true;
        //    result.Message = "Парсер запущен";
        //    result.NeedShowMessage = true;

        //    return result;
        //}

        //[HttpPost]
        //public Responce StopParsing()
        //{
        //    Responce result = new Responce();

        //    parserService.Stop();

        //    result.Success = true;
        //    result.Message = "Парсер остановлен";
        //    result.NeedShowMessage = true;

        //    return result;
        //}

        [HttpPut]
        [Route("ParserByInnStateToggle")]
        public Responce ParserByInnStateToggle()
        {
            ParserResponce result = new ParserResponce();

            if (parserService.ParserByInnState.State == (int)Enums.ParserState.Started)
            {
                result.ParserInfo.State = (int)Enums.ParserState.Undefined;
                parserService.StopParserByInn();
                //result.ParserInfo.IsBusy = false;
            }
            else
            {
                parserService.StartParserByInn();
                result.ParserInfo.State = (int)Enums.ParserState.Started;
            }
            

            result.Success = true;
            result.Message = "Успешное завершение операции";
            result.NeedShowMessage = false;

            return result;
        }
    }
}