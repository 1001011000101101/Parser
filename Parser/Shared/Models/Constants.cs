using System;
using System.Collections.Generic;
using System.Text;

namespace Parser.Shared.Models
{
    public class Constants
    {
        public static string DbFileLocation { get; } = @"Files\data.db";
        public static string WebDriverFolder { get; } = @"Files\";
        public static string UploadFilesFolder { get; } = @"UploadedFiles\";
        public static string RusProfileUrl { get; } = @"https://www.rusprofile.ru/";
    }
}
