using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using Microsoft.Extensions.Logging;
using Parser.Shared.Models;
using System.IO;

namespace Parser.Server.Code
{
    public static class Proxy
    {
        public static ConcurrentDictionary<string, int> All { get; private set; }
        public static ConcurrentDictionary<string, int> Checked { get; set; }
        public static ConcurrentBag<(string, long)> Fail { get; set; }

        private static object locker = new Object();

        public static DateTime ResreshDate;

        public static string AccessCode;

        public static void RefreshList(string accessCode)
        {
            AccessCode = accessCode;


            //log = Log;
            if (All == null)
            {
                All = new ConcurrentDictionary<string, int>();
            }
            else
            {
                All.Clear();
            }

            if (Checked == null)
            {
                Checked = new ConcurrentDictionary<string, int>();
            }
            else
            {
                Checked.Clear();
            }

            lock (locker)
            {
                Fail = new ConcurrentBag<(string, long)>();
            }






            

            //Console.WriteLine(html);





















            string proxyList = "";
            string proxyListUrl = Constants.ProxyListUrl;


            string html = GetRequest(Constants.ProxyListUrl);



            if (html.Length > 0)
            {
                //JObject jobject = JObject.Parse(json);
                //json = JsonConvert.DeserializeObject(data.TextContent);
                proxyList = html;
                //log.Category("Proxy").Info($"RefreshList. proxyList : {proxyList}");
            }
            else
            {
                return;
            }


  

            int errorCount = 0;
            foreach (string proxy in proxyList.Split(new string[] { "\r\n" }, StringSplitOptions.None))
            {
                All.TryAdd(proxy, errorCount);
            }
            ResreshDate = DateTime.Now;
        }

        public static int GetFailCount()
        {
            int count = 0;
            try
            {
                count = Proxy.Fail.DistinctBy(x => x.Item1).Count();
            }
            catch (Exception eee)
            {
                //log.Category("Proxy").Error(eee);
            }
            return count;
        }

        public static void get()
        {
            //log = Log;
            if (All == null)
            {
                All = new ConcurrentDictionary<string, int>();
            }
            else
            {
                All.Clear();
            }

            if (Checked == null)
            {
                Checked = new ConcurrentDictionary<string, int>();
            }
            else
            {
                Checked.Clear();
            }

            lock (locker)
            {
                Fail = new ConcurrentBag<(string, long)>();
            }

            string proxyList = GetRequest(Constants.ProxyListUrl);

            int errorCount = 0;
            foreach (string proxy in proxyList.Split(new string[] { "\r\n" }, StringSplitOptions.None))
            {
                All.TryAdd(proxy, errorCount);
            }
            ResreshDate = DateTime.Now;
        }


        public static string GetRequest(string url)
        {
            string html = string.Empty;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Constants.ProxyListUrl + AccessCode);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                html = reader.ReadToEnd();
            }

            return html;
        }

    }
}
