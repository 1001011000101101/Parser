using System;
using System.Collections.Generic;
using System.Text;

namespace Parser.Shared.Models
{
    public class ParserResponce: Responce
    {
        public ParserInfo ParserInfo { get; set; }

        public ParserResponce()
        {
            ParserInfo = new ParserInfo();
        }
    }
}
