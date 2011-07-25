using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Mite.Core;
using Mite.MsSql;
using Mite.MySql;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mite
{
    class Program
    {
        private static string miteConfigPath = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "mite.config";
        private static IDatabaseRepository repo;

        static void Main(string[] args)
        {
            
            if (args.Length == 0)
            {
                Console.WriteLine("You must specify an option.  See /? for details");
                return;
            }
            if (args[0] == "-v")
            {
                Console.WriteLine("Mite Version " + Assembly.GetExecutingAssembly().GetName().Version);
                return;
            }
#if DEBUG
            Console.WriteLine("Stopped in order to attach debugger.");
            Console.ReadLine();
#endif
            if (args[0] == "/?")
            {
                Console.WriteLine("Options are as follows:");
                Console.WriteLine("-v\tReturns the current version of Mite");
                Console.WriteLine(
                    "-d\tSpecifies the destination version to migrate to.  (can be greater than migrations available)");
                Console.WriteLine("update\tRuns all migrations greater than the current version");
                Console.WriteLine("-c\tCreates and launches the new migration files");
              //  Console.WriteLine("scratch\tdrops the database and recreates it using all the up scripts");
                Console.WriteLine("stepup\texecutes one migration file greater than the current version");
                Console.WriteLine("stepdown\texecutes one migration file less than the current version");
                Console.WriteLine(
                    "init Creates and opens the initial up file and makes.  Creates the _migrations table and makes and entry into the _migrations table for the initial up.");
                return;
            }
          if (args[0] == "-c")
            {
                if (!EnforceConfig()) return;
                CreateMigration();
                return;
            }
            if (args[0] == "init")
            {
                
                if (!File.Exists(miteConfigPath))
                {
                    Console.WriteLine("What provider are you using?");
                    Console.WriteLine("[1] MySql.Data.MySqlClient");
                    Console.WriteLine("[2] System.Data.SqlClient");
                    string providerName = "";
                    switch (Console.ReadLine()[0])
                    {
                        case '1':
                            providerName = "MySql.Data.MySqlClient";
                            break;
                        case '2':
                            providerName = "System.Data.SqlClient";
                            break;
                        default:
                            Console.WriteLine("Option not recognized");
                            return;
                    }
                    //determine the server
                    Console.WriteLine("Please enter you complete .Net connection string.");
                    string connectionString = Console.ReadLine();

                    //determine the database

                    JObject obj = new JObject();
                    obj["providerName"] = providerName;
                    obj["connectionString"] = connectionString;
                    File.WriteAllText(miteConfigPath, obj.ToString(Formatting.Indented));
                
                }
                var options = JObject.Parse(File.ReadAllText(miteConfigPath));
                repo = GetProvider(options.Value<string>("providerName"), options.Value<string>("connectionString"));
                var baseFileName = GetMigrationFileName();
                var baseFilePath = Environment.CurrentDirectory + Path.DirectorySeparatorChar + baseFileName+ ".sql";
                if (!File.Exists(baseFilePath))
                {
                    Console.WriteLine("Would you like me to generate a migration script based on the current database? [y|n]");
                    var generateScript = Console.ReadLine();
                    if (generateScript.ToLower() == "y")
                    {
                        bool includeData = false;
                        Console.WriteLine("Would you like to include the data? [y|n]");
                        var generateData = Console.ReadLine();
                        if (generateData.ToLower() == "y")
                        {
                            includeData = true;
                        }
                        var sql = repo.GenerateSqlScript(includeData);
                        File.WriteAllText(baseFilePath, sql);
                        Console.WriteLine(string.Format("{0} generated successfully", baseFileName));
                        repo.RecordMigration(new Migration(baseFileName, sql, ""));
                    }
                    else
                    {
                        Console.WriteLine("Use mite -c to create your first migration.");
                    }
                    return;
                }
                Console.WriteLine("Nothing to do.  Use mite -c to create your first migration.");
                }
            var config= JObject.Parse(File.ReadAllText(miteConfigPath));
            repo = GetProvider(config.Value<string>("providerName"), config.Value<string>("connectionString"));
            
            
            var database = repo.Create();
            var migrator = new Migrator(database, repo);

            if (!EnforceConfig()) return;
            MigrationResult resultingVersion = null;
            switch (args[0])
            {
                case "update":
                    if (database.IsValidState())
                    {
                        if (database.UnexcutedMigrations.Count() == 0)
                        {
                            Console.WriteLine("No migrations to execute");
                            Console.WriteLine("Current Version: " + database.Version);
                            return;
                        }
                        foreach (var migToExe in database.UnexcutedMigrations)
                        {
                            Console.WriteLine("Executing migration " + migToExe.Version);
                            repo.ExecuteUp(migToExe);
                        }
                    }
                    else if (database.IsMigrationGap())
                    {
                        Console.WriteLine("There is a gap in your migrations how would you like to resolve it?");
                        Resolve(migrator);
                    }
                    else if (database.IsHashMismatch())
                    {
                        Console.WriteLine("There is a mismatched checksum in your migrations, would you like me to resolve it? y|N\n This SHOULD NOT be performed in a production environment.");
                        if (Console.Read() == 'y')
                        {
                            Resolve(migrator);    
                        }
                    }
                    break;
                case "-d":
                    var version = args[1].Replace(".sql", "");
                    resultingVersion = migrator.MigrateTo(version);
                    Console.WriteLine("Current Version:" + resultingVersion.AfterMigration);
                    break;
                case "status":
                    Console.WriteLine("Current Version:" + database.Version);
                    if (database.IsValidState())
                    {
                        if (database.UnexcutedMigrations.Count() == 0)
                        {
                            Console.WriteLine("No migrations to execute");
                            return;
                        }
                    }
                    else
                    {
                        if (database.IsHashMismatch())
                        {
                            var invalidMigrations = database.InvalidMigrations();
                            Console.WriteLine("The following migrations don't match their checksums:");
                            foreach (var mig in invalidMigrations)
                            {
                                Console.WriteLine(mig.Version);
                            }
                            return;
                        }
                        if (database.IsMigrationGap())
                        {
                            Console.WriteLine("The following migrations have not been executed:");
                            foreach (var mig in database.UnexcutedMigrations.Where(x => x.Version.CompareTo(database.Version) <= 0))
                                Console.WriteLine(mig.Version);
                        }
                    }

                    Console.WriteLine("Unexecuted Migrations:");
                    foreach (var mig in database.UnexcutedMigrations)
                    {
                        Console.WriteLine(mig.Version);
                    }
                    return;
                case "stepdown":
                    resultingVersion = migrator.StepDown();
                    break;
                case "stepup":
                    resultingVersion = migrator.StepUp();
                    break;
                case "version":
                    Console.WriteLine("Database Version: " + database.Version);
                    return;
                case "scratch":
                    //drop all the tables and run all migrations
                    migrator.FromScratch();
                    Console.WriteLine("Database Version: " + database.Version);
                    break;
                case "clean":
                    Console.WriteLine("This will remove the mite.config and drop the _migrations table.  Are you sure you would like to clean (y/n)?");
                    if (Console.ReadLine() == "y")
                    {
                        repo.DropMigrationTable();
                        File.Delete(miteConfigPath);
                        Console.WriteLine("mite cleaned successfully");
                    }
                    return;
            }
            if (resultingVersion != null)
            {
                Console.WriteLine(resultingVersion.Message);
            }
        }

        private static IDatabaseRepository GetProvider(string providerName, string connectionString)
        {
            switch (providerName)
            {
                case "System.Data.SqlClient":
                    repo = new MsSqlDatabaseRepository(connectionString, Environment.CurrentDirectory);
                    break;
                case "MySql.Data.MySqlClient":
                    repo = new MySqlDatabaseRepository(connectionString, Environment.CurrentDirectory);
                    break;
                default:
                    Console.WriteLine("Provider not recognized.  Please check your mite.config");
                    break;
            }
            return repo;
        }

        private static void Resolve(Migrator migrator)
        {
            var result = migrator.SafeResolution();
            Console.WriteLine("Resolution Successful");
            Console.WriteLine("Current Database Version: " + result.AfterMigration);
        }

        private static bool EnforceConfig()
        {
            try
            {
                GetConnString();
                return true;
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("mite.config expected in current directory");
                return false;
            }
        }

        private static string GetConnString()
        {
            return File.ReadAllText(miteConfigPath);
        }


        private static string CreateMigration()
        {
            var executingDirectory = Environment.CurrentDirectory; 
            string baseName = GetMigrationFileName();
            var fileName = baseName + ".sql";
            var fullPath = executingDirectory + Path.DirectorySeparatorChar + fileName;
            var newlines = Environment.NewLine + Environment.NewLine;
            File.WriteAllText(fileName, "/* up */"+newlines+"/* down */" + newlines);
            Console.WriteLine("Creating file '{0}'", fullPath);
            Process.Start(fullPath);
            return baseName;
        }

        private static string GetMigrationFileName()
        {
            var now = DateTime.Now;
            return now.ToString("yyyy-MM-dd") + "T" + now.ToString("HH-mm-ss") + "Z";
        }
    }

}
