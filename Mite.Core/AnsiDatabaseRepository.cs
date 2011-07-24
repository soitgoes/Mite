using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Mite.Core
{
    public abstract class AnsiDatabaseRepository : IDatabaseRepository
    {
        protected string tableName;
        protected IDbConnection connection;
        protected string filePath;


        public virtual void DropMigrationTable()
        {
            this.connection.Open();
            var cmd = this.connection.CreateCommand();
            cmd.CommandText = string.Format("drop table {0}", tableName);
            cmd.ExecuteNonQuery();
            this.connection.Close();
        }

        public abstract bool MigrationTableExists();
        public abstract string GenerateSqlScript(bool includeData);

        public abstract MiteDatabase Init();

        public void Dispose()
        {
            connection.Dispose();
        }
        public virtual  bool CheckConnection()
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
        public virtual MiteDatabase Create()
        {
            //read all the migrations from the database and filesystem and create a MiteDatabase
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

        public virtual MiteDatabase RecordMigration(Migration migration)
        {
            if (!MigrationTableExists())
            {
                Init();
            }
            connection.Open();
            var cmd = GetMigrationCmd(migration);
            cmd.ExecuteNonQuery();
            connection.Close();
            return Create();
        }
        public virtual  MiteDatabase ExecuteUp(Migration migration)
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
                
                IDbCommand migrationCmd = GetMigrationCmd(migration);
                migrationCmd.Transaction = trans;
                migrationCmd.ExecuteNonQuery();
                trans.Commit();
            }
            connection.Close();            ;
            return Create();
        }

        protected virtual IDbCommand GetMigrationCmd(Migration migration)
        {
            var migrationCmd = connection.CreateCommand();
                
            migrationCmd.CommandText = string.Format("insert into {0} VALUES(@key, @hash)", tableName);
            IDbDataParameter key = migrationCmd.CreateParameter();
            key.Value = migration.Version;
            key.ParameterName = "key";
            IDbDataParameter hash = migrationCmd.CreateParameter();
            hash.Value = migration.Hash;
            hash.ParameterName = "hash";
            migrationCmd.Parameters.Add(key);
            migrationCmd.Parameters.Add(hash);
            return migrationCmd;
        }

        public virtual MiteDatabase ExecuteDown(Migration migration)
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
                var version = migrationCmd.CreateParameter();
                version.ParameterName = "version";
                version.Value = migration.Version;
                migrationCmd.Parameters.Add(version);
                migrationCmd.ExecuteNonQuery();
                trans.Commit();
            }
            connection.Close();
            return Create();
        }
    }
}