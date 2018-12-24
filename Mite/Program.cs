using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Mite.Builder;
using Mite.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mite
{
    class Program
    {
        private static readonly string currentDirectory = Environment.CurrentDirectory;
        private static IDatabaseRepository repo;

        static void Main(string[] args)
        {

            if (args.Length == 0)
            {
                Console.WriteLine("You must specify an option.  See /? for details");
                return;
            }
            if (args[0] == "/?")
            {
                Console.WriteLine("Mite - Simple and painless SQL migrations.\n\n"); 
                Console.WriteLine("Version " + Assembly.GetExecutingAssembly().GetName().Version);
                Console.WriteLine("mite.exe [-v] [init [filename]] [-c [filename]] [-d [destination]] [update/stepup/stepdown]\n\n");
                Console.WriteLine("Options are as follows:");
                Console.WriteLine("-v\t\tReturns the current version of Mite");
                Console.WriteLine(
                    "init\t\tCreates and opens the initial up file and makes.\n\t\tCreates the _migrations table and makes and entry into the \n\t\t_migrations table for the initial up.");
                Console.WriteLine("\t\tfilename(optional): desired filename of migration script");
                Console.WriteLine("-c\t\tCreates and launches the new migration files");
                Console.WriteLine("\t\tfilename(optional): desired filename of migration script");
                Console.WriteLine(
                    "-d\t\tSpecifies the destination version to migrate to.\n\t\t(can be greater than migrations available)");
                Console.WriteLine("update\t\tRuns all migrations greater than the current version");
                Console.WriteLine("stepup\t\tExecutes one migration file greater than the current version");
                Console.WriteLine("stepdown\tExecutes one migration file less than the current version");

                return;
            }

            if (args[0] == "-v")
            {
                Console.WriteLine("Mite Version " + Assembly.GetExecutingAssembly().GetName().Version);
                return;
            }
            if (args[0] == "-c")
            {
                try
                {
                    if (args.Count() > 1 && !string.IsNullOrEmpty(args[1]))
                    {
                        CreateMigration(args[1]);
                    }
                    else
                    {
                        CreateMigration();
                    }
                }
                catch (FormatException ex)
                {
                    Console.Write(ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }


                return;
            }
            if (args[0] == "init")
            {

                if (!File.Exists(currentDirectory))
                {
                    Console.WriteLine("What provider are you using?");
                    Console.WriteLine("[1] MySqlDatabaseRepository");
                    Console.WriteLine("[2] MsSqlDatabaseRepository");
                    string repositoryName = "";
                    switch (Console.ReadLine()[0])
                    {
                        case '1':
                            repositoryName = "MySqlDatabaseRepository";
                            break;
                        case '2':
                            repositoryName = "MsSqlDatabaseRepository";
                            break;
                        default:
                            Console.WriteLine("Option not recognized");
                            return;
                    }
                    //determine the server
                    Console.WriteLine("Please enter your complete .Net connection string.");
                    string connectionString = Console.ReadLine();

                    //determine the database
                    JObject obj = new JObject();
                    obj["repositoryName"] = repositoryName;
                    obj["connectionString"] = connectionString;
                    File.WriteAllText(Path.Combine(currentDirectory, "mite.config"), obj.ToString(Formatting.Indented));
                }

                try
                {
                    var tmpMigrator = MigratorFactory.GetMigrator(currentDirectory);
                    repo = tmpMigrator.DatabaseRepository;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }


                if (new DirectoryInfo(Environment.CurrentDirectory).GetFiles().Any(x => !x.Name.Contains("mite.config")))
                {
                    Console.WriteLine("Working directory is not clean.\nPlease ensure no existing scripts or project files exist when performing init.");
                    return;
                }


                var baseFileName = "";
                try
                {
                    if (args.Count() > 1 && !string.IsNullOrEmpty(args[1]))
                    {
                        baseFileName = GetMigrationFileName(args[1]);
                    }
                    else
                    {
                        baseFileName = GetMigrationFileName();
                    }
                }
                catch (FormatException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.Write(ex.Message);
                    return;
                }

                var baseFilePath = Environment.CurrentDirectory + Path.DirectorySeparatorChar + baseFileName;

                if (!File.Exists(baseFilePath))
                {
                    Console.WriteLine("Would you like me to generate a migration script based on the current database? [y|N]");
                    var generateScript = Console.ReadLine();
                    if (generateScript.ToLower() == "y")
                    {
                        bool includeData = false;
                        Console.WriteLine("Would you like to include the data? [y|N]");
                        var generateData = Console.ReadLine();
                        if (generateData.ToLower() == "y")
                        {
                            includeData = true;
                        }
                        try
                        {
                            var sql = repo.GenerateSqlScript(includeData);
                            File.WriteAllText(baseFilePath + ".sql", sql);
                            Console.WriteLine(string.Format("{0} generated successfully", baseFileName));
                            repo.RecordMigration(new Migration(baseFileName, sql, ""));
                        }
                        catch (Win32Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine("Using this feature requires that mysqldump be in your path.  Please add the path for mysqldump to your path variable and restart your cmd prompt.");
                        }

                    }
                    else
                    {
                        Console.WriteLine("Use mite -c to create your first migration.");
                    }
                    return;
                }
                Console.WriteLine("Nothing to do.  Use mite -c to create your first migration.");
            }

            var migrator = MigratorFactory.GetMigrator(currentDirectory);
            var database = migrator.Tracker;
            repo = migrator.DatabaseRepository;

            MigrationResult resultingVersion = null;
            switch (args[0])
            {
                case "update":
                    if (database.IsValidState())
                    {
                        if (!database.UnexcutedMigrations.Any())
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
                    var version = "";
                    if (args.Count() < 2)
                    {
                        Console.WriteLine("ERROR: You have failed to specify a destination version.\nPlease determine your destination version and try again.");
                        return;
                    }
                    version = args[1].Replace(".sql", "");
                    resultingVersion = migrator.MigrateTo(version);
                    Console.WriteLine("Current Version:" + resultingVersion.AfterMigration);
                    break;
                case "status":
                    Console.WriteLine("Current Version:" + database.Version);
                    if (database.IsValidState())
                    {
                        if (!database.UnexcutedMigrations.Any())
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
                                Console.WriteLine(mig.Version +  " [" + mig.Hash + "]");
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
                case "verify":
                    try
                    {
                        migrator.Verify();
                        Console.WriteLine("All migrations have been verified and executed successfully");
                    }
                    catch (MigrationException ex)
                    {
                        Console.WriteLine("Migrations could not be verified");
                        Console.WriteLine("The " + ex.Direction + " migration failed on: " + ex.Migration.Version);
                    }
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
                        File.Delete(currentDirectory);
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

        private static string CreateMigration(string scriptFileName = "")
        {
            var executingDirectory = Environment.CurrentDirectory;
            var baseName = GetMigrationFileName(scriptFileName);
            var fileName = baseName + ".sql";
            var fullPath = Path.Combine(executingDirectory, fileName);
            var newlines = Environment.NewLine + Environment.NewLine;
            File.WriteAllText(fileName, "/* up */" + newlines + "/* down */" + newlines);
            Console.WriteLine("Creating file '{0}'", fullPath);
            Process.Start(fullPath);
            return baseName;
        }

        private static bool FilenameIsValid(string scriptFilename)
        {
            bool isValid = true;
            DirectoryInfo taskDirectory = new DirectoryInfo(Environment.CurrentDirectory);

            FileInfo[] proposedFile = new FileInfo[] { new FileInfo(scriptFilename) };
            FileInfo[] proposedDirectoryContents = taskDirectory.GetFiles().Concat(proposedFile).Where(x => !x.Name.Contains("mite.config")).ToArray();

            Array.Sort(proposedDirectoryContents, (x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name));
            if (proposedDirectoryContents[proposedDirectoryContents.Count() - 1].Name != scriptFilename)
            {
                Console.WriteLine("The requested filename is invalid.  Filename must be ASCII greater than all other sql files in directory.\n");
                isValid = false;
            }
            return isValid;
        }

        private static string GetMigrationFileName(string scriptFilename = "")
        {
            var migrationScriptFilename = "";
            if (string.IsNullOrEmpty(scriptFilename))
            {
                var now = DateTime.Now;
                migrationScriptFilename = now.ToIso();
            }
            else
            {
                migrationScriptFilename = scriptFilename;
            }
            if (!FilenameIsValid(migrationScriptFilename))
            {
                throw new FormatException(string.Format("Error: Invalid Filename: {0}", migrationScriptFilename));
            }
            return migrationScriptFilename;
        }
    }

}
