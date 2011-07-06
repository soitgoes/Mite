using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Mite.Core;
using Mite.MsSql;
using Mite.MySql;

namespace Mite
{
    class Program
    {
        private static string miteConfigPath = Environment.CurrentDirectory + "\\mite.config";
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
                // Console.WriteLine("scratch\tdrops the database and recreates it using all the up scripts");
                Console.WriteLine("stepup\texecutes one migration file greater than the current version");
                Console.WriteLine("stepdown\texecutes one migration file less than the current version");
                Console.WriteLine(
                    "init Creates and opens the initial up file and makes.  Creates the _migrations table and makes and entry into the _migrations table for the initial up.");
                return;
            }
            string pathToOsql = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                                             "Microsoft SQL Server", "100", "Tools", "Binn", "OSQL.EXE");
            bool hasOsql = false; // File.Exists(pathToOsql);

            if (args[0] == "-c")
            {
                if (!EnforceConfig()) return;
                CreateMigration();
                return;
            }
            if (args[0] == "init")
            {

                if (args.Length != 2)
                {
                    Console.WriteLine("init requires one argument, the connection string of the database");
                    return;
                }
                repo = new MsSqlDatabaseRepository(args[1], Environment.CurrentDirectory);
                //repo = new MySqlDatabaseRepository(args[1], Environment.CurrentDirectory);
                if (repo.CheckConnection())
                {
                    File.WriteAllText(miteConfigPath, args[1]);
                }
                else
                {
                    Console.WriteLine("Connection Invalid");
                    return;
                }

                Console.WriteLine("Created _migrations table");
                repo.Init();
                return;
            }
            repo = new MsSqlDatabaseRepository(File.ReadAllText(miteConfigPath), Environment.CurrentDirectory);
            //repo = new MySqlDatabaseRepository(File.ReadAllText(miteConfigPath), Environment.CurrentDirectory);
            var database = repo.Create();
            var migrations = database.UnexcutedMigrations;

            var migrator = new Migrator(database, repo);

            if (!EnforceConfig()) return;
            MigrationResult resultingVersion = null;
            var unexecuted = database.UnexcutedMigrations;
            switch (args[0])
            {
                case "update":
                    if (database.IsValidState())
                    {
                        foreach (var migToExe in unexecuted)
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
                        if (migrations.Count() == 0)
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
                    foreach (var mig in migrations)
                    {
                        Console.WriteLine(mig.Version);
                    }
                    return;
                    break;
                case "stepdown":
                    resultingVersion = migrator.StepDown();
                    break;
                case "stepup":
                    resultingVersion = migrator.StepUp();
                    break;
                case "version":
                    Console.WriteLine("Database Version: " + database.Version);
                    return;
                    break;
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
                        File.Delete(Path.Combine(Environment.CurrentDirectory, "mite.config"));
                        Console.WriteLine("mite cleaned successfully");
                    }
                    return;
            }
            if (resultingVersion != null)
            {
                Console.WriteLine(resultingVersion.Message);
            }
        }

        private static void Resolve(Migrator migrator)
        {
            var result = migrator.SafeResolution();
            Console.WriteLine("Resolution Successful");
            Console.WriteLine("Current Database Version: " + result.AfterMigration);
        }

        private static bool EnforceConfig()
        {
            string connString = string.Empty;
            try
            {
                connString = GetConnString();
                return true;
            }
            catch (FileNotFoundException fnf)
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
            var executingDirectory = Environment.CurrentDirectory; //todo: ensure this is correct when executed.
            var now = DateTime.Now;
            var baseName = now.ToString("yyyy-MM-dd") + "T" + now.ToString("HH-mm-ss") + "Z";
            var fileName = baseName + ".sql";
            var fullPath = executingDirectory + "\\" + fileName;
            File.WriteAllText(fileName, "/* up */\r\n\r\n/* down */\r\n\r\n");
            Console.WriteLine("Creating file '{0}'", fullPath);
            Process.Start(fullPath);
            return baseName;
        }


    }

}
