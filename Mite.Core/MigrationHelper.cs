using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Mite.Core
{
    public static class MigrationHelper
    {
        public static IEnumerable<Migration> ReadFromDirectory(string directoryName)
        {
            var files = Directory.GetFiles(directoryName);
            var sqlMatch = new Regex("/\\* ?up ?\\*/(.*?)/\\* ?down ?\\*/(.*?)$", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (var file in files)
            {
                var info = new FileInfo(file);
                if (info.Name.StartsWith("_") || info.Name.ToLower().Equals("mite.config"))
                    continue;
                var sql = File.ReadAllText(file);
                var match= sqlMatch.Match(sql);
                var version = !string.IsNullOrEmpty(info.Extension) ? info.Name.Replace(info.Extension, "") : info.Name;
                if ( match.Success)
                {
                    yield return new Migration(version, match.Groups[1].Value, match.Groups[2].Value);    
                }else
                {
                    yield return new Migration(version, sql, "");
                }
                
            }
        }
    }
}