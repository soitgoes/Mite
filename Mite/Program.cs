using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Mite.Core;

namespace Mite {
    class Program {
        private static string miteConfigPath = Environment.CurrentDirectory + "\\mite.config";

        static void Main(string[] args)
        {
            if (args.Length == 0) {
                Console.WriteLine("You must specify an option.  See /? for details");
                return;
            }
            if (args[0] == "-v") {
                Console.WriteLine("Mite Version " + Assembly.GetExecutingAssembly().GetName().Version);
                return;
            }
            if (args[0] == "/?") {
                Console.WriteLine("Options are as follows:");
                Console.WriteLine("-v\tReturns the current version of Mite");
                Console.WriteLine(
                    "-d\tSpecifies the destination version to migrate to.  (can be greater than migrations available)");
                Console.WriteLine("update\tRuns all migrations greater than the current version");
                Console.WriteLine("-c\tCreates and launches the new migration files");
                // Console.WriteLine("scratch\tdrops the database and recreates it using all the up scripts");
                Console.WriteLine(
                    "init Creates and opens the initial up file and makes.  Creates the _migrations table and makes and entry into the _migrations table for the initial up.");
                return;
            }
            string pathToOsql = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                                             "Microsoft SQL Server", "100", "Tools", "Binn", "OSQL.EXE");
            bool hasOsql = false; // File.Exists(pathToOsql);
            if (args[0] == "-c") {
                if (!EnforceConfig()) return;
                CreateMigration(MigrationType.Down);
                CreateMigration(MigrationType.Up);
                return;
            }
            if (args[0] == "init") {

                if (args.Length != 2) {
                    Console.WriteLine("init requires one argument, the connection string of the database");
                    return;
                }
                if (CheckConnection(args[1])) {
                    File.WriteAllText(miteConfigPath, args[1]);
                } else {
                    return;
                }

                Console.WriteLine("Created _migrations table");
                using (var migrator = new Migrator(miteConfigPath, Environment.CurrentDirectory)) {
                    migrator.CreateMigrationTableIfNotExists();
                    var version = CreateMigration(MigrationType.Up);
                    migrator.SetCurrentVersion(version);
                }
                return;
            }

            using (var migrator = hasOsql
                                   ? new Migrator(pathToOsql, miteConfigPath, Environment.CurrentDirectory)
                                   : new Migrator(miteConfigPath, Environment.CurrentDirectory)) {


                if (!EnforceConfig()) return;
                MigrationResult resultingVersion = null;
                switch (args[0]) {
                    case "update":
                        resultingVersion = migrator.MigrateTo("999");
                        break;
                    case "-d":
                        resultingVersion = migrator.MigrateTo(args[1]);
                        break;
                    case "stepdown":
                        resultingVersion = migrator.StepDown();
                        break; 
                    case "stepup":
                        resultingVersion = migrator.StepUp();
                        break;
                    case "version":
                        Console.WriteLine("Database Version: " +migrator.GetCurrentVersion());
                        return;
                        break;
                    case "scratch":
                        //drop all the tables and run all migrations
                        migrator.FromScratch();
                        break;
                    case "clean":
                        Console.WriteLine("This will remove the mite.config and drop the _migrations table.  Are you sure you would like to clean (y/n)?");
                        if (Console.ReadLine() == "y") {
                            migrator.Cleanup();
                            Console.WriteLine("mite cleaned successfully");
                        }
                        return;
                }
                if (resultingVersion != null)
                {
                    Console.WriteLine(resultingVersion.Message);    
                }
            }
        }

        private static bool EnforceConfig() {
            string connString = string.Empty;
            try {
                connString = GetConnString();
            } catch (FileNotFoundException fnf) {
                Console.WriteLine("mite.config expected in current directory");

                return false;
            }
            if (!CheckConnection(connString)) {
                return false;
            }
            return true;
        }

        private static string GetConnString() {
            return File.ReadAllText(miteConfigPath);
        }

        private static bool CheckConnection(string connString) {
            try {
                using (var testConn = new SqlConnection(connString)) {
                    testConn.Open();
                    testConn.Close();
                }
                return true;
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Invalid Connection string.  Specify in mite.config or with mite init");
                return false;
            }

        }

        private static string CreateMigration(MigrationType typeOfMigration) {
            var executingDirectory = Environment.CurrentDirectory; //todo: ensure this is correct when executed.
            var now = DateTime.Now;
            var baseName = now.ToString("yyyy-MM-dd") + "T" + now.ToString("HH-mm-ss") + "Z";
            var fileName = baseName + string.Format("-{0}.sql", typeOfMigration.ToString().ToLower());
            var fullPath = executingDirectory + "\\" + fileName;
            File.WriteAllText(fileName, string.Format("/* put your {0} sql schema migration script here and then click save.*/", typeOfMigration));
            Console.WriteLine("Creating file '{0}'", fullPath);
            Process.Start(fullPath);
            return baseName;
        }


    }

}
