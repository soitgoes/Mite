using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

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
            var hashes = new Dictionary<string, string>();
            //read all the migrations from the database and filesystem and create a MiteDatabase
            //this.DatabaseExists() &&  
            if (this.MigrationTableExists())
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = string.Format("select * from {0}", tableName);
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        hashes.Add(dr["key"].ToString(), dr["hash"].ToString());
                    }
                }
                connection.Close();    
            }
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
        public virtual void CreateDatabase()
        {
            using(var conn = GetConnWithoutDatabaseSpecified())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "create database " + connection.Database;
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }
        public virtual void DropDatabase()
        {
            using (var conn = GetConnWithoutDatabaseSpecified())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "drop database " + connection.Database;
                cmd.ExecuteNonQuery();
                conn.Close();   
            }
        }

        public virtual  MiteDatabase ExecuteUp(Migration migration)
        {
            if (!MigrationTableExists())
                Init();
            connection.Open();
            using (var trans = connection.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                var split = new Regex(@"\sGO\s", RegexOptions.Multiline);
                foreach (var sql in split.Split(migration.UpSql))
                {
                    if (!string.IsNullOrEmpty(sql))
                    {
                        var cmd = connection.CreateCommand();
                        cmd.Transaction = trans;
                        cmd.CommandText = sql;
                        cmd.ExecuteNonQuery();    
                    }
                }
                IDbCommand migrationCmd = GetMigrationCmd(migration);
                migrationCmd.Transaction = trans;
                migrationCmd.ExecuteNonQuery();
                trans.Commit();
            }
            connection.Close();            ;
            return Create();
        }

        protected abstract IDbCommand GetMigrationCmd(Migration migration);
      

        public virtual MiteDatabase ExecuteDown(Migration migration)
        {
            if (!MigrationTableExists())
                Init();
            connection.Open();
            using (var trans = connection.BeginTransaction())
            {
                var split = new Regex(@"\sGO\s", RegexOptions.Multiline);
                foreach (var sql in split.Split(migration.DownSql))
                {
                    if (!string.IsNullOrEmpty(sql))
                    {
                        var cmd = connection.CreateCommand();
                        cmd.Transaction = trans;
                        cmd.CommandText = sql;
                        cmd.ExecuteNonQuery();    
                    }
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

        public string DatabaseName
        {
            get { return connection.Database; }
            set { connection.ChangeDatabase(value); }
        }

        protected abstract IDbConnection GetConnWithoutDatabaseSpecified();

        public virtual bool DatabaseExists()
        {
            bool result = false;
            using (var conn = GetConnWithoutDatabaseSpecified())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "use " + connection.Database;
                try
                {
                    cmd.ExecuteNonQuery();
                    result = true; 
                }
                catch (Exception ex)
                {
                    result = false;
                }finally
                {
                    conn.Close();
                }
                
            }
            return result;
        }
    }
}