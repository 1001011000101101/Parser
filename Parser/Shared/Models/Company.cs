﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Parser.Shared.Models
{
    public class Company
    {
        public int Id { get; set; }
        public string Inn { get; set; }
        public string Ogrn { get; set; }

        //Выручка (годовой оборот)
        public int AnnualIncome { get; set; }

        //Кредиторская задолженность
        public int Debt { get; set; }



        //public string Region { get; set; }
        //public string Okved { get; set; }
        //public int DebtPercent { get; set; }
        //public double ClaimAmount { get; set; }
        //public DateTime ClaimAmountPeriodStart { get; set; }
        //public DateTime ClaimAmountPeriodEnd { get; set; }
        //public int ClaimAmountPercent { get; set; }
    }
}
