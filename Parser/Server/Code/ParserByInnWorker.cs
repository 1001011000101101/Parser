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
        string ip = string.Empty;
        
        int failCount;
        private static object locker = new Object();

        public ParserByInnWorker(IHostEnvironment env, ILogger<ParserService> logger, IDb db, string proxyAccessCode) : base(env, logger, db, proxyAccessCode)
        {
        }
        public override void DoWork()
        {
            appSettings = db.GetSettings(env.ContentRootPath);
            companies = db.GetCompaniesFromExcel(env.ContentRootPath);

            ParserInfo.State = (int)Enums.ParserState.Started;
            KeyValuePair<string, int> pair = new KeyValuePair<string, int>();

            ChromeDriver browser = null;
            var chromeOptions = new ChromeOptions();
            chromeOptions = new ChromeOptions();
            //chromeOptions.AddArguments("headless");
            chromeOptions.AddArgument("--blink-settings=imagesEnabled=false");
            pair = GetKey();

            chromeOptions.AddArgument("--disable-application-cache");
            chromeOptions.AddArguments($"--proxy-server=socks4://{pair.Key}");

            browser = new ChromeDriver(Constants.WebDriverFolder, chromeOptions);



            logger.LogDebug($"ParserByInnWorker is starting.");


            ParserInfo.StateDescription = "ParserByInnWorker is starting";
            logger.LogDebug($"companies.Count = {companies.Count}");


            //parsingToken.Register(() =>
            //    logger.LogDebug($" GracePeriod background task is stopping."));

            for (int i = 0; i < companies.Count; i++)
            {
                bool polled = false;

                while (!polled)
                {
                    if (needStop)
                    {
                        Shutdown(browser);
                        ParserInfo.State = (int)Enums.ParserState.Stopped;
                        return;
                    }

                    try
                    {
                        IJavaScriptExecutor js = (IJavaScriptExecutor)browser;

                        var company = companies[i];
                        company.Inn = GetOnlyDigits(company.Inn);


                        ParserInfo.StateDescription = $"Work with {company.Inn} ({(i + 1)}/{companies.Count}). Get debt-to-income ratio proxy: {pair.Key}";

                        //Go to search page
                        browser.Navigate().GoToUrl(Constants.RusProfileUrl);


                        //var isCheckFormExists = js.ExecuteScript("var element = document.querySelector('#checkform'); return element != null; ")?.ToString();
                        //if (isCheckFormExists.Equals(Constants.True))
                        //{
                        //    string reCaptchaSource = browser.FindElement(By.CssSelector("#checkform iframe"))?.ToString();

                        //    //js.ExecuteScript($"document.querySelector('#recaptcha-anchor').click();");

                        //    var rtrtrt = "";

                        //}
                        //string pageSource = browser.PageSource;
                        //int pageSourceLength = browser.PageSource.Length;



                        var element = FindElement(browser, By.CssSelector("#indexsearchform input.index-search-input"), "Кнопка поиска", company);
                        element.Clear();
                        element.SendKeys(company.Inn);
                        js.ExecuteScript($"document.querySelector('#indexsearchform button.search-btn').click();");



                        //isCheckFormExists = js.ExecuteScript("var element = document.querySelector('#checkform'); return element != null; ")?.ToString();
                        //if (isCheckFormExists.Equals(Constants.True))
                        //{
                        //    string reCaptchaSource = browser.FindElement(By.CssSelector("#checkform iframe"))?.ToString();

                        //    //js.ExecuteScript($"document.querySelector('#recaptcha-anchor').click();");

                        //    var rtrtrt = "";

                        //}
                        //pageSource = browser.PageSource;
                        //pageSourceLength = browser.PageSource.Length;


                        element = FindElement(browser, By.CssSelector("#clip_ogrn"), "Поле ОРГН", company);



                        company.Ogrn = js.ExecuteScript("var element = document.querySelector('#clip_ogrn'); return element.innerHTML; ")?.ToString();

                        browser.Navigate().GoToUrl(Constants.RusProfileFinReportsUrl + company.Ogrn);

                        //isCheckFormExists = js.ExecuteScript("var element = document.querySelector('#checkform'); return element != null; ")?.ToString();
                        //if (isCheckFormExists.Equals(Constants.True))
                        //{
                        //    string reCaptchaSource = browser.FindElement(By.CssSelector("#checkform iframe"))?.ToString();

                        //    //js.ExecuteScript($"document.querySelector('#recaptcha-anchor').click();");

                        //    var rtrtrt = "";

                        //}
                        //pageSource = browser.PageSource;
                        //pageSourceLength = browser.PageSource.Length;

                        var isFinReportsExists = js.ExecuteScript("var element = document.querySelector('body.page404'); return element == null; ")?.ToString();




                        if (isFinReportsExists.Equals(Constants.True))
                        {
                            string annualIncome = string.Empty;
                            string debt = string.Empty;

                            annualIncome = js.ExecuteScript(" var es = document.querySelectorAll('div.tile-item__title'); for (var i = 0; i < es.length; i++) if (es[i].textContent.indexOf('Доходы и расходы по обычным видам деятельности') >= 0) return es[i].nextElementSibling.firstElementChild.firstElementChild.firstElementChild.lastElementChild.lastElementChild.innerHTML; return ''; ")?.ToString();
                            debt = js.ExecuteScript(" var es = document.querySelectorAll('div.tile-item__title'); for (var i = 0; i < es.length; i++) if (es[i].textContent.indexOf('Краткосрочные обязательства') >= 0) return es[i].nextElementSibling.firstElementChild.firstElementChild.firstElementChild.lastElementChild.lastElementChild.innerHTML; return ''; ")?.ToString();

                            company.AnnualIncome = int.Parse(GetOnlyDigits(annualIncome));
                            company.Debt = int.Parse(GetOnlyDigits(debt));
                        }




                        db.UpdateCompanies(new List<Company> { company }, env.ContentRootPath);
                        polled = true;

                    }
                    catch (Exception e)
                    {
                        Proxy.Fail.Add((pair.Key, 0));
                        logger.LogError($"ChromeDriver_ERROR {e.Message}");
                        ParserInfo.StateDescription = e.Message;


                        Shutdown(browser);

                        

                        pair = GetKey();



                        chromeOptions = new ChromeOptions();
                        //chromeOptions.AddArguments("headless");
                        chromeOptions.AddArgument("--blink-settings=imagesEnabled=false");
                        chromeOptions.AddArgument("--disable-application-cache");
                        chromeOptions.AddArguments($"--proxy-server=socks4://{pair.Key}");

                        browser = new ChromeDriver(Constants.WebDriverFolder, chromeOptions);
                    }
                    finally
                    {
                        //Shutdown(browser);
                    }




















                }













            }



            Shutdown(browser);

            ParserInfo.State = (int)Enums.ParserState.Stopped;
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

        protected IWebElement FindElement(ChromeDriver browser, By by, string elementDescripntion, Company company)
        {
            IWebElement element = browser.FindElement(by);
            if (element == null)
            {
                throw new Exception($"Что-то пошло не так. company.Inn {company.Inn} ERROR. элемент {elementDescripntion} на {Constants.RusProfileUrl} не найден [{DateTime.Now}]");
            }

            return element;
        }


        protected string GetOnlyDigits(string value)
        {
            Regex regex = new Regex(Constants.OnlyDigitsRegex, RegexOptions.IgnoreCase);
            Match match = regex.Match(value);

            if (match.Success)
            {
                return match.Groups[0].Value;
            }

            return string.Empty;
        }


        private KeyValuePair<string, int> GetKey()
        {
            int count = Proxy.All.Count();
            Random random = new Random();

            KeyValuePair<string, int> pair = new KeyValuePair<string, int>();
            string[] proxies = Proxy.Fail.Where(x => x.Item2 == 0).Select(x => x.Item1).ToArray(); // keys.ToArray();

            if (ip.Length > 0)
            {
                pair = new KeyValuePair<string, int>(ip, 0);
            }
            else if (failCount >= Constants.ProxyErrorLimit * 2)
            {
                pair = Proxy.Checked.FirstOrDefault(x => x.Value < Constants.ProxyErrorLimit && !proxies.Contains(x.Key));
                failCount = 0;
            }

            lock (locker)
            {
                if (pair.Key.IsNullOrEmpty() && /*proxies.Count()*/Proxy.Fail.Count() > 0 && Proxy.Fail.DistinctBy(x => x.Item1).Count() * 1.2 > Proxy.All.Count() /*Proxy.All.All(x => proxies.Contains(x.Key))*/)
                {
                    if ((DateTime.Now - Proxy.ResreshDate).TotalMinutes < 10)
                    {

                        Thread.Sleep(TimeSpan.FromMinutes(2));
                        return pair;
                    }
                    //log.Category("MeasuresHelper.ProxyRefresh").Info($"siteInCampaignID = {siteInCampaignID}");
                    Proxy.RefreshList(proxyAccessCode);
                }
            }

            //  int i = random.Next(0, count - 1);
            //        //pair = Proxy.List.FirstOrDefault(x => !proxies.Contains(x.Key) && x.Value < Settings.ProxyErrorLimit);
            //      pair = Proxy.All.ElementAt(i);


            int counter = 0;

            if (ip.IsNullOrEmpty() && pair.Key.IsNullOrEmpty())
            {
                while (pair.Key.IsNullOrEmpty() || proxies.Contains(pair.Key) || pair.Value >= Constants.ProxyErrorLimit)
                {
                    counter++;
                    if (counter > 1000)
                    {
                        //Proxy.RefreshList(NinjectConfig.Kernel.Get<ILog>());
                        pair = Proxy.All.FirstOrDefault();
                        break;
                    }

                    int i = random.Next(0, count - 1);
                    //pair = Proxy.List.FirstOrDefault(x => !proxies.Contains(x.Key) && x.Value < Settings.ProxyErrorLimit);
                    pair = Proxy.All.ElementAt(i);
                }
            }




            //if (pair.Key.IsNullOrEmpty())
            //{
            //    log.Category("MeasuresHelper.Poll.KeyIsNull").Error($"keys: {String.Join(";", proxies)}{Environment.NewLine}");
            //    log.Category("MeasuresHelper.Poll.KeyIsNull").Error($"List: {String.Join(";", Proxy.List.Select(x => new { Value = x.Key.ToString() + x.Value.ToString() }.Value))}{Environment.NewLine}");

            //    //Proxy.RefreshList(log);
            //    //keys.Clear();
            //}


            return pair;
        }
    }
}
