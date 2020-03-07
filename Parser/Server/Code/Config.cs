using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Parser.Server.Code
{
    public class Config:IConfig
    {
        protected readonly IConfigurationRoot configuration;

        public Config(IConfigurationRoot configuration)
        {
            this.configuration = configuration;
        }

        public string ProxyAccessCode
        {
            get
            {
                var code = configuration?["Proxy:AccessCode"];
                return string.IsNullOrEmpty(code) ? null : code;
            }
        }
    }
}
