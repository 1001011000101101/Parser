using System;
using System.Collections.Generic;
using System.Text;

namespace Parser.Shared.Models
{
    public class CompaniesResponce : Responce
    {
        public bool CompaniesFileIsUploaded { get; set; }
        public string CompaniesFileName { get; set; }
        public DateTime CompaniesFileUploadedDate { get; set; }
        public int CompaniesCount { get; set; }
    }
}
