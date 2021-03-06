﻿using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using NLog.Web;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Parser.Shared.Models;
using System.Text;
using Parser.Server.Code;

namespace Parser.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();

            try
            {
                logger.Debug("init main");

                //https://stackoverflow.com/questions/47851858/stg-e-writefault-while-reading-excel-file-with-epplus
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                if (!System.IO.Directory.Exists(Constants.UploadFilesFolder))
                {
                    System.IO.Directory.CreateDirectory(Constants.UploadFilesFolder);
                }

                if (!System.IO.Directory.Exists(Constants.TempFilesFolder))
                {
                    System.IO.Directory.CreateDirectory(Constants.TempFilesFolder);
                }


                BuildWebHost(args).Run();


            }
            catch (Exception exception)
            {
                //NLog: catch setup errors
                logger.Error(exception, "Stopped program because of exception");
                return; throw;
            }
            finally
            {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                NLog.LogManager.Shutdown();
            }
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseConfiguration(new ConfigurationBuilder()
                    .AddCommandLine(args)
                .AddJsonFile("appsettings.json", true)
                    .Build())
                .UseStartup<Startup>().UseDefaultServiceProvider(options =>
            options.ValidateScopes = false)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Trace);
            })
            .UseNLog()
                .Build();
    }
}
