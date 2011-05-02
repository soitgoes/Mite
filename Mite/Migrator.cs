using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace Mite
{
    public class Migrator
    {
        private static string migrationTable = "_migration";

        private readonly IDbConnection connection;

        public Migrator(IDbConnection connection)
        {
            this.connection = connection;
        }

        public void Migrate(ConsoleOptions options)
        {

            //if the table does exists then create it
            CreateMigrationTableIfNotExists();
            string currentVersion = GetCurrentVersion();
            //get all the sql scripts in the current or specified directory that it is executed and sort them by alpha
            using (var cmd  = connection.CreateCommand())
            {
                foreach (var sql in GetUpScripts())
                {

                }
    
            }
            
            //execute all the sql scripts that contain "up" and are in greater string order than the current app and less than the version specified (if any)



        }
        public string GetCurrentVersion()
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = string.Format("select top 1 migration_key from {0} order by migration_key desc",
                                                migrationTable);
             return cmd.ExecuteScalar().ToString();
            }
        }
        public IEnumerable<string> GetUpScripts(string currentVersion, string destinationVersion)
        {
            var files = Directory.GetFiles("*.sql");
            foreach (var file in files)
            {
                var fi = new FileInfo(file);
                if (fi.Name.Contains("-up") && string.Compare(fi.Name, currentVersion)  && string.Compare(destinationVersion, fi.Name))
                {
                    return File.ReadAllText(file); 
                }
            }
        }


        private void CreateMigrationTableIfNotExists()
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
        }
    }
}
