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
//using Newtonsoft.Json;



namespace Parser.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly ILogger<SettingsController> logger;
        private IHostEnvironment env;

        public SettingsController(ILogger<SettingsController> logger, IHostEnvironment env)
        {
            this.logger = logger;
            this.env = env;
        }

        [HttpGet]
        public Settings Get()
        {
            using (var db = new LiteDatabase(System.IO.Path.Combine(env.ContentRootPath, Constants.dbFileLocation)))
            {
                // Get a collection (or create, if doesn't exist)
                var settings = db.GetCollection<Settings>("settings");

                // Use LINQ to query documents
                return settings.FindAll().SingleOrDefault();
            }

            
        }

        [HttpPost]
        public Responce SaveSettingsAsync(Settings settings)
        {
            Responce result = new Responce();



            // Open database (or create if doesn't exist)
            using (var db = new LiteDatabase(System.IO.Path.Combine(env.ContentRootPath, Constants.dbFileLocation)))
            {
                // Get a collection (or create, if doesn't exist)
                var col = db.GetCollection<Settings>("settings");


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
    }
}