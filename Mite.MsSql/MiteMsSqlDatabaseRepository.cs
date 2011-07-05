using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Mite.Core;
namespace Mite.MsSql
{
    public class MiteMsSqlDatabaseRepository: IMiteDatabaseRepository
    {
        private readonly string filePath;
        private readonly string tableName;
        private readonly SqlConnection connection;
        public MiteMsSqlDatabaseRepository(string connectionString, string filePath):this(connectionString, filePath, "_migrations")
        {            
        }
        public MiteMsSqlDatabaseRepository(string connectionString, string filePath, string tableName)
        {
            this.filePath = filePath;
            this.tableName = tableName;
            connection = new SqlConnection(connectionString);
        }

        public MiteDatabase Init()
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

            var cmd = connection.CreateCommand();
            cmd.CommandText = migrationTableScript;
            cmd.ExecuteNonQuery();
            return Create();
        }

        public MiteDatabase Create()
        {
            //read all the migrations from the database and filesystem and create a MiteDatabase
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = string.Format("select * from {0}", tableName);
            Dictionary<string, string> hashes = new Dictionary<string, string>();
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    hashes.Add(dr["key"].ToString(), dr["hash"].ToString());    
                }
            }
            connection.Close();
            return new MiteDatabase(MigrationHelper.ReadFromDirectory(filePath).ToList(), hashes);
        }
        public string GetCurrentVersion ()
        {
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = string.Format("select top 1 key from {0} order by key desc", tableName);
            var version = cmd.ExecuteScalar().ToString();
            connection.Close();
            return version;
        }
        public MiteDatabase ExecuteUp(Migration migration)
        {
            connection.Open();
            using (var trans = connection.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                foreach (var sql in migration.UpSql.Split(new string[] { "GO" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var cmd = connection.CreateCommand();
                    cmd.Transaction = trans;
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
                
                var migrationCmd = connection.CreateCommand();
                migrationCmd.Transaction = trans;
                migrationCmd.CommandText = string.Format("insert into {0} VALUES(@key, @hash)", tableName);
                migrationCmd.Parameters.AddWithValue("@key", migration.Version);
                migrationCmd.Parameters.AddWithValue("@hash", migration.Hash);
                migrationCmd.ExecuteNonQuery();
                trans.Commit();
            }
            connection.Close();            ;
            return Create();
        }

        public MiteDatabase ExecuteDown(Migration migration)
        {
            connection.Open();
            using (var trans = connection.BeginTransaction())
            {
                foreach (var sql in migration.DownSql.Split(new string[] { "GO" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var cmd = connection.CreateCommand();
                    cmd.Transaction = trans;
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }

                var migrationCmd = connection.CreateCommand();
                migrationCmd.Transaction = trans;
                migrationCmd.CommandText = string.Format("delete from {0} where [key] = @version", tableName);
                migrationCmd.Parameters.AddWithValue("@version", migration.Version);
                migrationCmd.ExecuteNonQuery();
                trans.Commit();
            }
            connection.Close();
            return Create();
        }

        public bool CheckConnection()
        {
            try
            {
                this.connection.Open();
                return true;
            }catch(SqlException ex)
            {
                return false;
            }
            finally
            {
                connection.Close();
            }
        }

        public void DropMigrationTable()
        {
            this.connection.Open();
            var cmd = this.connection.CreateCommand();
            cmd.CommandText = string.Format("drop table {0}", tableName);
            cmd.ExecuteNonQuery();
            this.connection.Close();
        }


        public void Dispose()
        {
            connection.Dispose();            
        }
    }
}
