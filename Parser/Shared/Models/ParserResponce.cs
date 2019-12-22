using System;
using System.Collections.Generic;
using System.Text;

namespace Parser.Shared.Models
{
    public class ParserResponce: Responce
    {
        public ParserState ParserState { get; set; }

        public ParserResponce()
        {
            ParserState = new ParserState();
        }
    }
}
