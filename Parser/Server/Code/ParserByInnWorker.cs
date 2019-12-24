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
using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;

namespace Parser.Server.Code
{
    public class ParserByInnWorker : BaseWorker
    {
        public ParserByInnWorker(IHostEnvironment env, ILogger<ParserService> logger) : base(env, logger)
        {
        }
        public override void DoWork()
        {
            ChromeDriver browser = null;

            try
            {
                logger.LogDebug($"ParserByInnWorker is starting.");
                State.IsBusy = true;

                State.Description = "ParserByInnWorker is starting";
                logger.LogDebug($"companies.Count = {companies.Count}");


                //parsingToken.Register(() =>
                //    logger.LogDebug($" GracePeriod background task is stopping."));

                for (int i = 0; i < companies.Count; i++)
                {
                    if (needStop)
                    {
                        State.IsBusy = false;
                        return;
                    }

                    var chromeOptions = new ChromeOptions();
                    //chromeOptions.AddArguments("headless");
                    //chromeOptions.AddArgument("--blink-settings=imagesEnabled=false");
                    chromeOptions.AddArgument("--disable-application-cache");
                    browser = new ChromeDriver(Constants.WebDriverFolder, chromeOptions);
                    IJavaScriptExecutor js = (IJavaScriptExecutor)browser;

                    var company = companies[i];


                    Regex regex = new Regex(Constants.OnlyDigitsRegex, RegexOptions.IgnoreCase);
                    Match match = regex.Match(company.Inn);
  

                    if (match.Success)
                    {
                        company.Inn = match.Groups[0].Value;
                    }


                    State.Description = $"Work with {company.Inn} ({(i+1)}/{companies.Count}). Get debt-to-income ratio";
                    

                    browser.Navigate().GoToUrl(Constants.RusProfileUrl);

                    //recaptcha-anchor
                    var elementRecaptcha = js.ExecuteScript("var element = document.querySelector('#rc-anchor-container'); return element ");
                    browser.FindElement(By.CssSelector("#rc-anchor-container"));
                    if (elementRecaptcha != null)
                    {
                        js.ExecuteScript($"document.querySelector('##recaptcha-anchor').click();");


                    }

                    var element = browser.FindElement(By.CssSelector("#indexsearchform input.index-search-input"));
                    element.Clear();
                    element.SendKeys(company.Inn);
                    js.ExecuteScript($"document.querySelector('#indexsearchform button.search-btn').click();");

                    company.Ogrn = js.ExecuteScript("var element = document.querySelector('#clip_ogrn'); return element.innerHTML; ")?.ToString();

                    browser.Navigate().GoToUrl(Constants.RusProfileFinReportsUrl + company.Ogrn);
                    var isFinReportsExists = js.ExecuteScript("var element = document.querySelector('body.page404'); return element == null; ")?.ToString();

                    UpdateCompanies(new List<Company> { company });


                    if (isFinReportsExists.Equals(Constants.True))
                    {

                    }

                    var rtrtrt = "";
                    //await Task.Delay(5000, parsingToken);
                    Thread.Sleep(10000);

                    Shutdown(browser);
                }
            }
            catch (Exception e)
            {
                logger.LogError($"ChromeDriver_ERROR {e.Message}");
            }
            finally
            {
                Shutdown(browser);
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



            logger.LogDebug($"ParserBaseService background task is stopping.");

        }

        private void Shutdown(ChromeDriver browser)
        {
            if (browser != null)
            {
                try
                {
                    browser.Close();
                }
                catch (Exception eee) { logger.LogDebug("MeasuresHelper.Poll.Dispose.browser.Close()"); }

                try
                {
                    browser.Quit();
                }
                catch (Exception eee) { logger.LogDebug("MeasuresHelper.Poll.Dispose.browser.Quit()"); }

                try
                {
                    browser.Dispose();
                }
                catch (Exception eee) { logger.LogDebug("MeasuresHelper.Poll.Dispose.browser.Dispose()"); }
            }

            //if (document != null)
            //{
            //    try
            //    {
            //        document.Dispose();
            //    }
            //    catch (Exception eee) { log.Category("MeasuresHelper.Poll.Dispose.document.Dispose()").Error(eee); }


            //}
            //DisposeBath();

            //https://github.com/SeleniumHQ/selenium/issues/2056
            //foreach (var process in Process.GetProcessesByName("firefox"))
            //    {
            //        try
            //        {
            //            string commandLine = process.GetCommandLine();

            //            if (guid.IsNotNullOrEmpty() && commandLine.Contains(guid))
            //            {
            //                var parentID = process.Parent().Id;
            //                KillProcessAndChildren(parentID);

            //            }

            //            var yuj = "";
            //        }
            //        catch (Win32Exception ex) when ((uint)ex.ErrorCode == 0x80004005)
            //        {
            //            // Intentionally empty - no security access to the process.
            //        }
            //        catch (InvalidOperationException)
            //        {
            //            // Intentionally empty - the process exited before getting details.
            //        }
            //    }


            //if (requesterID == (int)RequestersEnum.Selenium)
            //{
            //    try
            //    {
            //        foreach (var p in Process.GetProcessesByName("w3wp"))
            //        {
            //            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + p.Id);
            //            ManagementObjectCollection moc = searcher.Get();
            //            foreach (ManagementObject mo in moc)
            //            {
            //                var process = Process.GetProcessById(Convert.ToInt32(mo["ProcessID"]));
            //                TimeSpan interval = DateTime.Now - process.StartTime;
            //                if (interval.TotalMinutes > 5)
            //                {
            //                    try
            //                    {
            //                        process.Kill();
            //                    }
            //                    catch (Exception) { }

            //                }
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        log.Category("MeasuresHelper.MeasuresHelper.KillOlder15Minutes").Error(ex);
            //    }
            //}


            if (true)
            {
                List<Process> processes = new List<Process>();
                //processes.AddRange(Process.GetProcessesByName("firefox"));
                //processes.AddRange(Process.GetProcessesByName("geckodriver"));
                processes.AddRange(Process.GetProcessesByName("chrome"));
                processes.AddRange(Process.GetProcessesByName("chromedriver"));
                //processes.AddRange(Process.GetProcessesByName("cmd"));

                foreach (var process in processes)
                {
                    try
                    {
                        TimeSpan interval = DateTime.Now - process.StartTime;

                        if (interval.TotalMinutes > 2)
                        {
                            KillProcessAndChildren(process.Id);
                            //ExecuteCommand($"Taskkill /PID {process.Id} /F");
                            //ExecuteCommand($"Taskkill /IM chromedriver.exe /F");
                        }
                        var yuj = "";
                    }
                    catch (Exception eee)
                    {
                        logger.LogDebug("MeasuresHelper.Kill");
                    }

                    //catch (Win32Exception ex) when ((uint)ex.ErrorCode == 0x80004005)
                    //{
                    //    // Intentionally empty - no security access to the process.
                    //}
                    //catch (InvalidOperationException)
                    //{
                    //    // Intentionally empty - the process exited before getting details.
                    //}
                }
            }


        }

        //private void DisposeBath()
        //{
        //    if (objectsToDispose == null || objectsToDispose.Count == 0) return;
        //    foreach (IDisposable x in objectsToDispose)
        //    {
        //        if (x != null)
        //        {
        //            try
        //            {
        //                x.Dispose();
        //            }
        //            catch (Exception eee) { log.Category("MeasuresHelper.Poll.DisposeBath()").Error(eee); }
        //        }
        //    }
        //}

        /// <summary>
        /// Kill a process, and all of its children.
        /// </summary>
        /// <param name="pid">Process ID.</param>
        private static void KillProcessAndChildren(int pid)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));

            }
            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
                //proc.WaitForExit(1000 * 60);
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }

        public void ExecuteCommand(string Command)
        {
            ProcessStartInfo ProcessInfo;
            Process Process;

            ProcessInfo = new ProcessStartInfo("cmd.exe", "/K " + Command);
            ProcessInfo.CreateNoWindow = true;
            ProcessInfo.UseShellExecute = false;

            Process = Process.Start(ProcessInfo);
        }

    }
}
