using System.Collections.Generic;
using System.IO;

namespace Mite.Core
{
    public static class MigrationHelper
    {
        public static IEnumerable<Migration> ReadFromDirectory(string directoryName)
        {
            var files = Directory.GetFiles(directoryName,  "*.sql");
            foreach (var file in files)
            {
                var sql = File.ReadAllText(file);
                var info = new FileInfo(file);
                var type = info.Name.Contains("-up") ? MigrationType.Up : MigrationType.Down;
                var version = info.Name.Replace("-up", "")
                    .Replace("-down", "")
                    .Replace(".sql", "");
                yield return new Migration(version, type, sql);
            }
        }
    }
}