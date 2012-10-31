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
        protected string delimiter = @"\sGO\s";

        
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

        public abstract MigrationTracker Init();

        public void Dispose()
        {
            connection.Dispose();
        }


        public IDbConnection Connection
        {
            get { return connection; }
            set { connection = value; }
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
        public virtual MigrationTracker Create()
        {
            if (!DatabaseExists())
                CreateDatabaseIfNotExists();
            var hashes = new Dictionary<string, string>();
            //read all the migrations from the database and filesystem and create a MigrationTracker
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
            return new MigrationTracker(MigrationHelper.ReadFromDirectory(filePath).ToList(), hashes);
        }

        

        public virtual MigrationTracker RecordMigration(Migration migration)
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
        /// <summary>
        /// Create a database if it doesn't exists.  Throw exception if the database already exists.  Used to create a temporary database for verification
        /// </summary>
        public abstract void CreateDatabaseIfNotExists();

        public virtual void DropDatabase()
        {
            using (var conn = GetConnWithoutDatabaseSpecified())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "drop database " + this.DatabaseName;
                cmd.ExecuteNonQuery();
                conn.Close();   
            }
        }

        public virtual  MigrationTracker ExecuteUp(Migration migration)
        {
            if (!MigrationTableExists())
                Init();
            connection.Open();
            using (var trans = connection.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                var split = new Regex(delimiter, RegexOptions.Multiline);
                var statements = split.Split(migration.UpSql);
                foreach (var sql in statements)
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
      

        public virtual MigrationTracker ExecuteDown(Migration migration)
        {
            if (!MigrationTableExists())
                Init();
            connection.Open();
            using (var trans = connection.BeginTransaction())
            {
                var split = new Regex(delimiter, RegexOptions.Multiline);
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
            set
            {
                connection.Open();
                connection.ChangeDatabase(value);
                connection.Close();
            }
        }

        public abstract IDbConnection GetConnWithoutDatabaseSpecified();

        public virtual bool DatabaseExists()
        {
            bool result = false;
            using (var conn = GetConnWithoutDatabaseSpecified())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "use " + DatabaseName;
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