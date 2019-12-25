using System;
using System.Collections.Generic;
using System.Text;
using Parser.Shared.Models;


namespace Parser.Server.Code
{
    public interface IDb
    {
        Settings GetSettings(string contentRootPath);
        List<Company> GetCompaniesFromExcel(string contentRootPath);
        void UpdateCompanies(List<Company> companies, string contentRootPath);
    }
}
