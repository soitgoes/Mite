using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

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
            //TODO: change so that the provider is specified in the mite.config so that it can be cross database compatible.
            this.connection = new SqlConnection(connectionString);
            this.connection.Open();
        }

        public MigrationResult MigrateTo(string destinationVersion) {
            //if the table does exists then create it
            CreateMigrationTableIfNotExists();
            string currentVersion = GetCurrentVersion();
            return MigrateFrom(currentVersion, destinationVersion);
        }

        private MigrationResult MigrateFrom(string currentVersion, string destinationVersion) {
            if (currentVersion.CompareTo(destinationVersion) == 0) //nothing to do
                return new MigrationResult(false, "Database is already at destination version"); ;
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
            if (plan.SqlToExecute.Length > 0)
            {
              SetCurrentVersion(plan.DestinationVersion, plan.SqlToExecute.Last());  
            }
            Console.WriteLine("Number of scripts executed: " + scriptsExecuted);
            return new MigrationResult(true,  plan.OriginVersion, plan.DestinationVersion);
        }

        public void SetCurrentVersion(string currentVersion, string sql)
        {
          var crypto = new SHA1CryptoServiceProvider();
          var hash = Convert.ToBase64String(crypto.ComputeHash(Encoding.UTF8.GetBytes(sql)));
            using (var cmd = connection.CreateCommand()) {
              cmd.CommandText = string.Format("insert into {0} ([migration_key], [hash], [performed]) VALUES ('{1}', '{2}', getDate())",
                                                migrationTable, currentVersion, hash);
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
              //TODO complete.
                //SetCurrentVersion("", );
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
                            [hash] [nvarchar](50) NOT NULL,
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

        public MigrationResult StepDown()
        {
            var version = GetCurrentVersion();
            Migration destination= MigrationHelper.ReadFromDirectory(this.scriptDirectory).Where(x=> x.Version.CompareTo(version) <= 0).OrderBy(x => x.Version).FirstOrDefault();
            var destinationVersion = destination== null ? version : destination.Version;
            return this.MigrateTo(destinationVersion);
        }
        public MigrationResult StepUp()
        {
            var version = GetCurrentVersion();
            Migration destination = MigrationHelper.ReadFromDirectory(this.scriptDirectory).Where(x => x.Version.CompareTo(version) > 0).OrderBy(x => x.Version).FirstOrDefault();
            var destinationVersion = destination == null ? version : destination.Version;
            return this.MigrateTo(destinationVersion);
        } 
    }
}
