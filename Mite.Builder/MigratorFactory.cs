using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Mite.Core;
using Newtonsoft.Json.Linq;

namespace Mite.Builder {
    public static class MigratorFactory {
        public static Migrator GetMigratorFromConfig(string config, string directoryPath) {
            var databaseRepositories = InstancesOf<IDatabaseRepository>();
            var jobj = JObject.Parse(config);
            var repoName = jobj.Value<string>("repositoryName");
            var connString = jobj.Value<string>("connectionString");
            object[] args = new object[]{connString, directoryPath};
            foreach (var repoType in databaseRepositories)
            {
                if (repoName.ToLower() == repoType.Name.ToLower())
                {
                    var dynamicRepo = (IDatabaseRepository)Activator.CreateInstance(repoType, BindingFlags.CreateInstance, null, args, null);
                    var miteDb = dynamicRepo.Create();
                    return new Migrator(miteDb, dynamicRepo);
                }                    
            }
            throw new Exception("No database repositories match the name: " + repoName);
        }

        public static Migrator GetMigrator(string directoryName) {
            var miteConfigPath = Path.Combine(directoryName, "mite.config");
            if (File.Exists(miteConfigPath)) {
                return GetMigratorFromConfig(File.ReadAllText(miteConfigPath), directoryName);
            } else {
                throw new FileNotFoundException("mite.config is not contained in the directory specified");
            }
        }

        private static IEnumerable<Type> InstancesOf<T>()
        {
            var executingDirectory= Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var files = Directory.GetFiles(executingDirectory, "*.dll", SearchOption.TopDirectoryOnly);
            var type = typeof(T);
            return files.Select(Assembly.LoadFrom).SelectMany(a => (from t in a.GetExportedTypes()
                                                                            where t.IsClass
                                                                                  && type.IsAssignableFrom(t)
                                                                            select t));
        }
    }
}
