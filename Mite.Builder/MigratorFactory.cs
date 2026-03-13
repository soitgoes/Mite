using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mite.Core;
using Mite.MsSql;
using Mite.MySql;
using Newtonsoft.Json.Linq;

namespace Mite.Builder {
    public static class MigratorFactory {

        public static Migrator GetMigratorFromConfig(string config, string directoryPath, string connectionName = null) {
            var options = JObject.Parse(config);

            string repoName;
            string connString;

            var connectionsToken = options["connections"];
            if (connectionsToken != null && connectionsToken.Type == JTokenType.Object)
            {
                var connections = (JObject)connectionsToken;
                if (string.IsNullOrEmpty(connectionName))
                    connectionName = options.Value<string>("default");

                if (string.IsNullOrEmpty(connectionName))
                {
                    if (connections.Properties().Count() == 1)
                        connectionName = connections.Properties().First().Name;
                    else
                        throw new Exception("Multiple connections defined — specify one with -n <name> or set a \"default\" in mite.config.");
                }

                var conn = connections[connectionName] as JObject;
                if (conn == null)
                {
                    var available = string.Join(", ", connections.Properties().Select(p => p.Name));
                    throw new Exception($"Connection '{connectionName}' not found in mite.config. Available: {available}");
                }

                repoName = conn.Value<string>("repositoryName");
                connString = conn.Value<string>("connectionString");
            }
            else
            {
                if (!string.IsNullOrEmpty(connectionName))
                    throw new Exception($"Connection name '{connectionName}' specified but mite.config uses the legacy flat format. Run 'mite init -n {connectionName}' to add named connections.");

                repoName = options.Value<string>("repositoryName");
                connString = options.Value<string>("connectionString");
            }

            if (string.IsNullOrEmpty(repoName)) {
                throw new Exception("Invalid Config - repositoryName is required.");
            }
            if (string.IsNullOrEmpty(connString)) {
                throw new Exception("Invalid Config - connectionString is required.");
            }

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

        public static Migrator GetMigrator(string directoryName, string connectionName = null) {
            var miteConfigPath = Path.Combine(directoryName, "mite.config");
            if (File.Exists(miteConfigPath)) {
                return GetMigratorFromConfig(File.ReadAllText(miteConfigPath), directoryName, connectionName);
            }
            throw new FileNotFoundException("mite.config is not contained in the directory specified");
        }

        public static string[] GetConnectionNames(string directoryName)
        {
            var miteConfigPath = Path.Combine(directoryName, "mite.config");
            if (!File.Exists(miteConfigPath))
                return new string[0];

            var options = JObject.Parse(File.ReadAllText(miteConfigPath));
            var connectionsToken = options["connections"];
            if (connectionsToken != null && connectionsToken.Type == JTokenType.Object)
                return ((JObject)connectionsToken).Properties().Select(p => p.Name).ToArray();

            return new string[0];
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
