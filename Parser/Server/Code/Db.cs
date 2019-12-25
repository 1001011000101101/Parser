using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using OfficeOpenXml;
using Parser.Shared.Models;

namespace Parser.Server.Code
{
    public class Db : IDb
    {

        public Settings GetSettings(string contentRootPath)
        {
            using (var db = new LiteDatabase(Path.Combine(contentRootPath, Constants.DbFileLocation)))
            {
                var settings = db.GetCollection<Settings>("settings");

                var s = settings.FindAll().SingleOrDefault();
                if (s == null)
                {
                    settings.Insert(new Settings());
                }

                return settings.FindAll().SingleOrDefault();
            }
        }

        public List<Company> GetCompaniesFromExcel(string contentRootPath)
        {
            List<Company> c = new List<Company>();
            var settings = GetSettings(contentRootPath);

            string path = Path.Combine(contentRootPath, Constants.UploadFilesFolder, settings.CompaniesFileName);
            using (var fs = new FileStream(path, System.IO.FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var excelPackage = new ExcelPackage(fs))
                {
                    var excelWorkbook = excelPackage.Workbook;
                    var sheet = excelWorkbook.Worksheets[1];

                    int colCount = sheet.Dimension.End.Column;  //get Column Count
                    int rowCount = sheet.Dimension.End.Row;     //get row count
                    for (int row = 1; row <= rowCount; row++)
                    {
                        if (sheet.Cells[row, 1].Value == null) continue;

                        c.Add(new Company()
                        {
                            Inn = sheet.Cells[row, 1].Value.ToString()
                        });
                    }
                }
            }

            return c;
        }


        public void UpdateCompanies(List<Company> companies, string contentRootPath)
        {
            using (var db = new LiteDatabase(Path.Combine(contentRootPath, Constants.DbFileLocation)))
            {
                // Get a collection (or create, if doesn't exist)
                var col = db.GetCollection<Company>("companies");

                foreach (var c in companies)
                {
                    var company = col.FindOne(x => x.Inn.Equals(c.Inn));

                    if (company == null)
                    {
                        col.Insert(c);
                    }
                    else
                    {
                        col.Update(company);
                    }

                    col.EnsureIndex(x => x.Inn);
                }
            }
        }
    }
}
