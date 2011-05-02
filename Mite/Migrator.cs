using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace Mite
{
    public class Migrator : IDisposable
    {
        private readonly string configLocation;
        private readonly string connectionString;
        private static string migrationTable = "_migrations";
        public string MigrationTable { get { return migrationTable; } set { migrationTable = value; } }
        private readonly IDbConnection connection;
        private readonly string scriptDirectory;

        public Migrator(IDbConnection connection, string scriptDirectory)
        {
            this.connection = connection;
            this.scriptDirectory = scriptDirectory;
            this.connection.Open();
        }
        public Migrator(string configLocation, string scriptDirectory)
        {
            this.configLocation = configLocation;
            this.scriptDirectory = scriptDirectory;
            this.connectionString = File.ReadAllText(configLocation);
            this.connection = new SqlConnection(connectionString);
            this.connection.Open();
        }

        public string MigrateTo(string destinationVersion)
        {
            //if the table does exists then create it
            CreateMigrationTableIfNotExists();
            string currentVersion = GetCurrentVersion();
            return MigrateFrom(currentVersion, destinationVersion);    
        }

        private string MigrateFrom(string currentVersion, string destinationVersion)
        {
            if (currentVersion.CompareTo(destinationVersion) == 0) //nothing to do
                return "nothing to do";
            int scriptsExecuted = 0;
            string lastVersion = currentVersion;
            foreach (var scripts in GetScripts(currentVersion, destinationVersion))
            {
                string script = scripts.Value;
                Console.WriteLine("\tExecuting Script: " + scripts.Key);
                foreach (var sql in script.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        cmd.ExecuteNonQuery();
                    }
                }
                
                lastVersion = scripts.Key;
                SetCurrentVersion(scripts.Key);
            }
            Console.WriteLine("Number of scripts executed: " + scriptsExecuted);
            return lastVersion;
        }

        public void SetCurrentVersion(string currentVersion)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = string.Format("insert into {0} VALUES ('{1}', getDate())",
                                                migrationTable, currentVersion);
                var tmp = cmd.ExecuteNonQuery();             
            }    
        }

        public void Cleanup()
        {
            File.Delete(configLocation); //Delete the configuration file
            //drop the _migrations table
            DropMigrationTable();
        }
        public void FromScratch()
        {
            //close connection
            this.connection.Close();
            using (var scratchConnection= new SqlConnection(connectionString.Replace(connection.Database, "master")))
            {
                //drop the database
                var database = this.connection.Database;
                scratchConnection.Open();
                using (var cmd = scratchConnection.CreateCommand())
                {
                    cmd.CommandText = "drop database " + database;
                    cmd.ExecuteNonQuery();
                }
                //create the database
                using (var cmd = this.connection.CreateCommand())
                {
                    cmd.CommandText = "create database " + database;
                    cmd.ExecuteNonQuery();
                }
                SetCurrentVersion("");
                this.MigrateTo("999");    

            }

        }

        private void DropMigrationTable()
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = string.Format("drop table {0}",
                                                migrationTable);
                cmd.ExecuteNonQuery();
            }
        }

        public string GetCurrentVersion()
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = string.Format("select top 1 migration_key from {0} order by migration_key desc",
                                                migrationTable);
                var tmp = cmd.ExecuteScalar();
                return tmp == null ? "" : tmp.ToString();
            }
        }
        public Dictionary<string, string> GetScripts(string currentVersion, string destinationVersion)
        {
            var postFix = destinationVersion.CompareTo(currentVersion) < 0 ? "-up" : "-down";
            var upScripts = new Dictionary<string, string>();
            var files = Directory.GetFiles(scriptDirectory, "*.sql");
            foreach (var file in files)
            {
                var fi = new FileInfo(file);
                string version = fi.Name.Replace("-up", "").Replace(".sql", "");
                bool isGreaterThanCurrent = string.Compare(version, currentVersion) > 0;
                bool isLessThanDestination = string.Compare(version, destinationVersion) < 0;
                if (fi.Name.Contains("-up") && isGreaterThanCurrent && isLessThanDestination)
                {
                    upScripts.Add(version, File.ReadAllText(fi.Name));
                }
            }
            return upScripts;
        }


        public void CreateMigrationTableIfNotExists()
        {
            using (var cmd = connection.CreateCommand())
            {
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
            //using (var cmd = connection.CreateCommand())
            //{
            //    cmd.CommandText = string.Format("INSERT INTO {0} VALUES ('0000', getDate())", migrationTable);
            //    cmd.ExecuteNonQuery();
            //}
        }

        public void Dispose()
        {
            connection.Dispose();
        }
    }
}
