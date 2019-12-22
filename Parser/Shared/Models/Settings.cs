using System;
using System.Collections.Generic;
using System.Text;

namespace Parser.Shared.Models
{
    public class Settings
    {
        public int Id { get; set; }
        public string Region { get; set; }
        public string Okved { get; set; }
        public int DebtPercent { get; set; }
        public double ClaimAmount { get; set; }
        public DateTime ClaimAmountPeriodStart { get; set; }
        public DateTime ClaimAmountPeriodEnd { get; set; }
        public int ClaimAmountPercent { get; set; }
        public bool CompaniesFileIsUploaded { get; set; }
        public string CompaniesFileName { get; set; }
        public DateTime CompaniesFileUploadedDate { get; set; }

    }
}
