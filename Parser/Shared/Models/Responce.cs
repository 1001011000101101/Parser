using System;
using System.Collections.Generic;
using System.Text;

namespace Parser.Shared.Models
{
    public class Responce
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public bool NeedShowMessage { get; set; }
    }
}
