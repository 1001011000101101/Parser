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
        public static string TempFilesFolder { get; } = @"TempFiles\";
        public static string RusProfileUrl { get; } = @"https://www.rusprofile.ru/";
        public static string RusProfileFinReportsUrl { get; } = @"https://www.rusprofile.ru/accounting?ogrn=";

        public static string True { get; } = @"True";
        public static string False { get; } = @"False";

        public static string OnlyDigitsRegex = @"[0-9]+";
    }
}
