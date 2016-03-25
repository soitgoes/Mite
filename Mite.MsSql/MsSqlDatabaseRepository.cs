using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Mite.Core;

namespace Mite.MsSql
{
    /// <summary>
    /// Sql Server Database Repository
    /// </summary>
    /// <seealso cref="Mite.Core.AnsiDatabaseRepository" />
    public class MsSqlDatabaseRepository: AnsiDatabaseRepository
    {
        private const string DefaultMigrationsTable = "_migrations";
        private Server server;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsSqlDatabaseRepository"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="filePath">The file path.</param>
        public MsSqlDatabaseRepository(string connectionString, string filePath):this(connectionString, filePath, DefaultMigrationsTable)
        {            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MsSqlDatabaseRepository"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="tableName">Name of the table.</param>
        public MsSqlDatabaseRepository(string connectionString, string filePath, string tableName)
        {
            this.filePath = filePath;
            this.tableName = tableName;
            var connString = new SqlConnectionStringBuilder(connectionString) {PersistSecurityInfo = true};
            connection = new SqlConnection(){ ConnectionString = connString.ToString()};
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MsSqlDatabaseRepository"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="assembly">The assembly.</param>
        /// <param name="pattern">The pattern.</param>
        public MsSqlDatabaseRepository(string connectionString, Assembly assembly, string pattern) : this(connectionString, assembly, pattern, DefaultMigrationsTable)
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MsSqlDatabaseRepository"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="assembly">The assembly.</param>
        /// <param name="pattern">The pattern.</param>
        /// <param name="tableName">Name of the table.</param>
        public MsSqlDatabaseRepository(string connectionString, Assembly assembly, string pattern, string tableName)
        {
            this.assembly = assembly;
            this.assemblyPattern = pattern;
            this.tableName = tableName;
            var connString = new SqlConnectionStringBuilder(connectionString) { PersistSecurityInfo = true };
            connection = new SqlConnection() { ConnectionString = connString.ToString() };
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <returns></returns>
        public override MigrationTracker Init()
        {
            var migrationTableScript =
                @"/* To prevent any potential data loss issues, you should review this script in detail before running it outside the context of the database designer.*/
BEGIN TRANSACTION
SET QUOTED_IDENTIFIER ON
SET ARITHABORT ON
SET NUMERIC_ROUNDABORT OFF
SET CONCAT_NULL_YIELDS_NULL ON
SET ANSI_NULLS ON
SET ANSI_PADDING ON
SET ANSI_WARNINGS ON
COMMIT
BEGIN TRANSACTION
GO
CREATE TABLE dbo._migrations
	(
	[key] nvarchar(20) NOT NULL,
	hash nvarchar(50) NOT NULL
	)  ON [PRIMARY]
GO
ALTER TABLE dbo._migrations ADD CONSTRAINT
	PK__migrations PRIMARY KEY CLUSTERED 
	(
	[key]
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
CREATE UNIQUE NONCLUSTERED INDEX IX__migrations ON dbo._migrations
	(
	[key]
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
ALTER TABLE dbo._migrations SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
select Has_Perms_By_Name(N'dbo._migrations', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'dbo._migrations', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'dbo._migrations', 'Object', 'CONTROL') as Contr_Per";

            this.connection.Open();
            foreach (var sql in migrationTableScript.Split(new string[] { "GO" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
            this.connection.Close();
            return Create();
        }


        /// <summary>
        /// Migrations the table exists.
        /// </summary>
        /// <returns></returns>
        public override bool MigrationTableExists()
        {
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = string.Format(@"SELECT 1 FROM sysobjects WHERE id = object_id(N'[dbo].[{0}]')", tableName);
            var dr = cmd.ExecuteReader();
            var result = dr.Read();
            connection.Close();
            return result;
        }

        /// <summary>
        /// Gets the connection without database specified.
        /// </summary>
        /// <returns></returns>
        public override IDbConnection GetConnWithoutDatabaseSpecified()
        {
            var csb = new SqlConnectionStringBuilder(connection.ConnectionString);
            csb["Database"] = "master";
            return new SqlConnection(csb.ConnectionString);
        }

        /// <summary>
        /// Generates the SQL script.
        /// </summary>
        /// <param name="includeData">if set to <c>true</c> [include data].</param>
        /// <returns></returns>
        public override string GenerateSqlScript(bool includeData)
        {
            var serverConn = new ServerConnection((SqlConnection)connection);
            server = new Server(serverConn);
            var db = new Database(server, connection.Database);
            List<Urn> list = new List<Urn>();
            DataTable dataTable = db.EnumObjects(DatabaseObjectTypes.Table);
            foreach (DataRow row in dataTable.Rows)
            {
                list.Add(new Urn((string)row["Urn"]));
            }

            Scripter scripter = new Scripter();
            scripter.Server = server;
            scripter.Options.IncludeDatabaseContext = false;
            scripter.Options.IncludeHeaders = true;
            scripter.Options.SchemaQualify = true;
            scripter.Options.ScriptData = includeData;            
            scripter.Options.SchemaQualifyForeignKeysReferences = true;
            scripter.Options.NoCollation = true;
            scripter.Options.DriAllConstraints = true;
            scripter.Options.DriAll = true;
            scripter.Options.DriAllKeys = true;
            scripter.Options.Triggers = true;
            scripter.Options.DriIndexes = true;
            scripter.Options.ClusteredIndexes = true;
            scripter.Options.NonClusteredIndexes = true;
            scripter.Options.ToFileOnly = false;
            var scripts = scripter.EnumScript(list.ToArray());
            string result = "";
            foreach (var script in scripts)
                result += script + Environment.NewLine;
            serverConn.Disconnect();
            return result;
        }

        /// <summary>
        /// Create a database if it doesn't exists.  Throw exception if the database already exists.  Used to create a temporary database for verification
        /// </summary>
        public override void CreateDatabaseIfNotExists()
        {
            using (var cnn= GetConnWithoutDatabaseSpecified())
            {
                cnn.Open();
                var cmd = cnn.CreateCommand();
                cmd.CommandText = string.Format(@"if not exists(select * from sys.databases where name = '{0}') create database {0}", DatabaseName);
                cmd.ExecuteNonQuery();
                cnn.Close();    
            }            
        }

        /// <summary>
        /// Gets the migration command.
        /// </summary>
        /// <param name="migration">The migration.</param>
        /// <returns></returns>
        protected override IDbCommand GetMigrationCmd(Migration migration)
        {

            var migrationCmd = (SqlCommand)connection.CreateCommand();
            migrationCmd.CommandText = string.Format("insert into {0} VALUES(@key, @hash)", tableName);
            migrationCmd.Parameters.AddWithValue("@key", migration.Version);
            migrationCmd.Parameters.AddWithValue("@hash", migration.Hash);
            return migrationCmd;
        }

        /// <summary>
        /// Executes the script.
        /// </summary>
        /// <param name="script">The script.</param>
        public void ExecuteScript(string script)
        {
            if (string.IsNullOrWhiteSpace(script))
                return;

            connection.Open();
            using (var trans = connection.BeginTransaction())
            {
                var scriptParts = Regex.Split(script, "^GO", RegexOptions.Multiline);

                foreach (var sql in scriptParts)
                {
                    if (string.IsNullOrWhiteSpace(sql))
                        continue;

                    var cmd = connection.CreateCommand();
                    cmd.Transaction = trans;
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
                trans.Commit();
            }
            connection.Close();
        }
    }
}
