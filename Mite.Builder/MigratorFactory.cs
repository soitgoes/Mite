using System;
using System.Collections.Generic;
using System.IO;
using Mite.Core;
using Mite.MsSql;
using Mite.MySql;
using Newtonsoft.Json.Linq;

namespace Mite.Builder {
    public static class MigratorFactory {
        public static Migrator GetMigratorFromConfig(string config, string directoryPath) {
            var databaseRepositories = new List<Type>() {  typeof(MySqlDatabaseRepository), typeof(MsSqlDatabaseRepository) };
            var options = JObject.Parse(config);
            var repoName = options.Value<string>("repositoryName");
            var connString = options.Value<string>("connectionString");

            if (string.IsNullOrEmpty(repoName)) {
                throw new Exception("Invalid Config - repositoryName is required.");
            }
            if (string.IsNullOrEmpty(connString)) {
                throw new Exception("Invalid Config - connectionString is required.");
            }
            object[] args = new object[]{connString, directoryPath};
            IDatabaseRepository databaseRepository = null;
            switch (repoName)
            {
                case "MySqlDatabaseRepository":
                    databaseRepository = new MySqlDatabaseRepository(connString, directoryPath);
                    break;
                case "MsSqlDatabaseRepository":
                    databaseRepository = new MsSqlDatabaseRepository(connString, directoryPath);
                    break;
               
            }
            var miteDb = databaseRepository.Create();
            return new Migrator(miteDb, databaseRepository);
        }

        public static Migrator GetMigrator(string directoryName) {
            var miteConfigPath = Path.Combine(directoryName, "mite.config");
            if (File.Exists(miteConfigPath)) {
                return GetMigratorFromConfig(File.ReadAllText(miteConfigPath), directoryName);
            }
            throw new FileNotFoundException("mite.config is not contained in the directory specified");
        }

        public static Migrator GetMigrator(IDatabaseRepository databaseRepository, string directoryPath)
        {
            var tracker = databaseRepository.Create();
            tracker.Permissive = true;
            return new Migrator(tracker, databaseRepository);
        }

        //private static IEnumerable<Type> InstancesOf<T>()
        //{
        //    var executingDirectory= Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        //    var files = Directory.GetFiles(executingDirectory, "*.dll", SearchOption.TopDirectoryOnly);
        //    var type = typeof(T);
        //    return files.Select(Assembly.LoadFrom).SelectMany(a => (from t in a.GetExportedTypes()
        //                                                                    where t.IsClass
        //                                                                          && type.IsAssignableFrom(t)
        //                                                                    select t));
        //}
    }
}
