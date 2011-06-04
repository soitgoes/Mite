using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

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
            var direction = destinationVersion.CompareTo(currentVersion) > 0 ? MigrationType.Up : MigrationType.Down;
            int scriptsExecuted = 0;
            string lastVersion = currentVersion;
            var allScripts = GetScripts(currentVersion, destinationVersion, direction);
            for (int i=0 ; i< allScripts.Count ; i++)
            {
                var scripts = allScripts.AsQueryable().ElementAt(i);
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
                if (direction == MigrationType.Down)
                {
                    SetCurrentVersion(allScripts.AsQueryable().ElementAt(i+1).Key);    
                }else
                {
                    SetCurrentVersion(scripts.Key);    
                }
                lastVersion = scripts.Key;                
                scriptsExecuted++;
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
        public Dictionary<string, string> GetScripts(string currentVersion, string destinationVersion, MigrationType direction)
        {
            var postFix = "-" + direction.ToString().ToLower();
            var scripts = new Dictionary<string, string>();
            IEnumerable<string> files = new List<string>(Directory.GetFiles(scriptDirectory, "*.sql"));
            if (direction == MigrationType.Down)
                files = files.OrderByDescending(y => y);
            foreach (var file in files)
            {
                var fi = new FileInfo(file);
                string version = fi.Name.Replace(postFix, "").Replace(".sql", "");
                if (fi.Name.Contains(postFix))
                {
                    bool isGreaterThanCurrent = string.Compare(version, currentVersion) > 0;
                    bool isLessThanDestination = string.Compare(version, destinationVersion) < 0;
                    if (direction == MigrationType.Up && isGreaterThanCurrent && isLessThanDestination)
                    {
                        scripts.Add(version, File.ReadAllText(fi.Name));    
                    }else if (direction == MigrationType.Down && !isLessThanDestination && !isGreaterThanCurrent)
                    {
                        scripts.Add(version, File.ReadAllText(fi.Name));    
                    }
                }
            }            
            return scripts;
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
