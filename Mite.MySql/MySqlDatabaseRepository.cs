using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Mite.Core;
using MySql.Data.MySqlClient;

namespace Mite.MySql
{
    public class MySqlDatabaseRepository : IDatabaseRepository
    {
        private readonly string filePath;
        private readonly string tableName;
        private readonly MySqlConnection connection;

        public MySqlDatabaseRepository(string connectionString, string filePath):this(connectionString, filePath, "_migrations")
        {
        }
        public MySqlDatabaseRepository (string connectionString, string filePath, string tableName)
        {
            this.filePath = filePath;
            this.tableName = tableName;
            this.connection = new MySqlConnection(connectionString);
        }

        public void Dispose()
        {
            connection.Dispose();
        }

        public MiteDatabase Init()
        {
            var migrationTableScript =
                @"CREATE  TABLE `_migrations` (
  `key` VARCHAR(20) NOT NULL ,
  `hash` VARCHAR(50) NOT NULL ,
  PRIMARY KEY (`key`) ,
  UNIQUE INDEX `hash_UNIQUE` (`hash` ASC) );";
            this.connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = migrationTableScript;
            cmd.ExecuteNonQuery();
            this.connection.Close();
            return Create();
        }

        public MiteDatabase Create()
        {
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = string.Format("select * from {0}", tableName);
            var hashes = new Dictionary<string, string>();
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
                migrationCmd.CommandText = string.Format("insert into {0} VALUES(?key, ?hash)", tableName);
                migrationCmd.Parameters.Add("?key", migration.Version);
                migrationCmd.Parameters.Add("?hash", migration.Hash);
                migrationCmd.ExecuteNonQuery();
                trans.Commit();
            }
            connection.Close(); ;
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
                migrationCmd.CommandText = string.Format("delete from {0} where `key` = ?version", tableName);
                migrationCmd.Parameters.Add("?version", migration.Version);
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
            }
            catch (Exception ex)
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
        public void ExecuteScript(string script)
        {
            connection.Open();
            using (var trans = connection.BeginTransaction())
            {
                foreach (var sql in script.Split(new string[] { "GO" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var cmd = connection.CreateCommand();
                    cmd.Transaction = trans;
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
                trans.Commit();
            }
            connection.Close();
        }

        public bool MigrationTableExists()
        {
            this.connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "show tables";
            var dr = cmd.ExecuteReader();
            var tables = new List<string>();
            while (dr.Read())
            {
                tables.Add(dr[0].ToString());
            }
            this.connection.Close();
            return tables.Contains(tableName);
        }

        public string GenerateSqlScript(bool includeData)
        {
            throw new NotImplementedException();
        }
    }
}
