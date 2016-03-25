using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Mite.Core
{
    /// <summary>
    /// Migration Helper
    /// </summary>
    public static class MigrationHelper
    {
        /// <summary>
        /// Maps the specified file name.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="sql">The SQL.</param>
        /// <returns></returns>
        private static Migration Map(string fileName, string sql)
        {
            var sqlMatch = new Regex("/\\* ?up ?\\*/(.*?)/\\* ?down ?\\*/(.*?)$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var match = sqlMatch.Match(sql);
            var ext = Path.GetExtension(fileName);
            var version = !string.IsNullOrEmpty(ext) ? fileName.Replace(ext, "") : fileName;

            return match.Success ? new Migration(version, match.Groups[1].Value, match.Groups[2].Value) : new Migration(version, sql, "");
        }

        /// <summary>
        /// Reads migration files from a directory.
        /// </summary>
        /// <param name="directoryName">Name of the directory.</param>
        /// <returns></returns>
        public static IEnumerable<Migration> ReadFromDirectory(string directoryName)
        {
            var files = Directory.GetFiles(directoryName, "*.sql",  SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                var info = new FileInfo(file);
                if (info.Name.StartsWith("_") || info.Name.ToLower().Equals("mite.config"))
                    continue;

                var sql = File.ReadAllText(file);
                yield return Map(info.Name, sql);
            }
        }

        /// <summary>
        /// Reads migration files from an assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="nameSpace">The name space.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static IEnumerable<Migration> ReadFromAssembly(Assembly assembly, string nameSpace)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            var resourceNames = assembly.GetManifestResourceNames();

            foreach (var resourceName in resourceNames)
            {
                if (!resourceName.StartsWith(nameSpace, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                using (var reader = new StreamReader(stream))
                {
                    var fileName = Path.GetFileNameWithoutExtension(resourceName.Substring(nameSpace.Length).Trim('.'));
                    var sql = reader.ReadToEnd();
                    yield return Map(fileName, sql);
                }
            }
        }
    }
}