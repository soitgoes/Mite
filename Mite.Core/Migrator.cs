using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Mite.Core {
    public class Migrator : IDisposable {
        private readonly string configLocation;
        private readonly string connectionString;
        private static string migrationTable = "_migrations";
        public string MigrationTable { get { return migrationTable; } set { migrationTable = value; } }
        private readonly IDbConnection connection;
        private readonly string scriptDirectory;
        private string pathToOsql;
        private bool hasOsql = false;

        public Migrator(string pathToOsql, string configLocation, string scriptDirectory) {
            this.configLocation = configLocation;
            this.pathToOsql = pathToOsql;
            this.scriptDirectory = scriptDirectory;
            this.hasOsql = true;
        }

        public Migrator(IDbConnection connection, string scriptDirectory) {
            this.connection = connection;
            this.scriptDirectory = scriptDirectory;
            this.connection.Open();
        }
        public Migrator(string configLocation, string scriptDirectory) {
            this.configLocation = configLocation;
            this.scriptDirectory = scriptDirectory;
            this.connectionString = File.ReadAllText(configLocation);
            this.connection = new SqlConnection(connectionString);
            this.connection.Open();
        }

        public string MigrateTo(string destinationVersion) {
            //if the table does exists then create it
            CreateMigrationTableIfNotExists();
            string currentVersion = GetCurrentVersion();
            return MigrateFrom(currentVersion, destinationVersion);
        }

        private string MigrateFrom(string currentVersion, string destinationVersion) {
            if (currentVersion.CompareTo(destinationVersion) == 0) //nothing to do
                return "nothing to do";
            var direction = destinationVersion.CompareTo(currentVersion) > 0 ? MigrationType.Up : MigrationType.Down;
            int scriptsExecuted = 0;
            var migrations = MigrationHelper.ReadFromDirectory(Environment.CurrentDirectory);
            var plan = migrations.GetMigrationPlan(currentVersion, destinationVersion);
            var allScripts = plan.SqlToExecute;
            foreach (var script in allScripts) {
                //Console.WriteLine("\tExecuting {0} Script: \n\n" + script, direction);
                if (hasOsql) {
                    Environment.CurrentDirectory = scriptDirectory;
                    var process = new Process();
                    var info = new ProcessStartInfo(pathToOsql, script);
                    info.RedirectStandardOutput = true;
                    process.StartInfo = info;
                    process.Start();
                } else {
                    foreach (var sql in script.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries)) {
                        using (var cmd = connection.CreateCommand()) {
                            cmd.CommandText = sql;
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                scriptsExecuted++;
            }
            SetCurrentVersion(plan.DestinationVersion);
            Console.WriteLine("Number of scripts executed: " + scriptsExecuted);
            return plan.DestinationVersion;
        }

        public void SetCurrentVersion(string currentVersion) {
            using (var cmd = connection.CreateCommand()) {
                cmd.CommandText = string.Format("insert into {0} VALUES ('{1}', getDate())",
                                                migrationTable, currentVersion);
                var tmp = cmd.ExecuteNonQuery();
            }
        }

        public void Cleanup() {
            File.Delete(configLocation); //Delete the configuration file
            //drop the _migrations table
            DropMigrationTable();
        }
        public void FromScratch() {
            //close connection
            this.connection.Close();
            using (var scratchConnection = new SqlConnection(connectionString.Replace(connection.Database, "master"))) {
                //drop the database
                var database = this.connection.Database;
                scratchConnection.Open();
                using (var cmd = scratchConnection.CreateCommand()) {
                    cmd.CommandText = "drop database " + database;
                    cmd.ExecuteNonQuery();
                }
                //create the database
                using (var cmd = this.connection.CreateCommand()) {
                    cmd.CommandText = "create database " + database;
                    cmd.ExecuteNonQuery();
                }
                SetCurrentVersion("");
                this.MigrateTo("999");

            }

        }

        private void DropMigrationTable() {
            using (var cmd = connection.CreateCommand()) {
                cmd.CommandText = string.Format("drop table {0}",
                                                migrationTable);
                cmd.ExecuteNonQuery();
            }
        }

        public string GetCurrentVersion() {
            using (var cmd = connection.CreateCommand()) {
                cmd.CommandText = string.Format("select top 1 migration_key from {0} order by id desc",
                                                migrationTable);
                var tmp = cmd.ExecuteScalar();
                return tmp == null ? "" : tmp.ToString();
            }
        }


        public void CreateMigrationTableIfNotExists() {
            using (var cmd = connection.CreateCommand()) {
                //TODO replace with ANSI sql for broader compatibility
                cmd.CommandText = @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[_migrations]') AND type in (N'U'))
                        BEGIN
                        CREATE TABLE [dbo].[_migrations](
	                        [id] [int] IDENTITY(1,1) NOT NULL,
	                        [migration_key] [nvarchar](50) NOT NULL,
	                        [performed] [datetime] NOT NULL,
                         CONSTRAINT [PK__migrations] PRIMARY KEY CLUSTERED 
                        (
	                        [id] ASC
                        )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
                        ) ON [PRIMARY]
                        END";
                cmd.ExecuteNonQuery();
            }
        }

        public void Dispose() {
            connection.Dispose();
        }
    }
}
