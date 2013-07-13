using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Mite.Core;
using MySql.Data.MySqlClient;

namespace Mite.MySql
{
    public class MySqlDatabaseRepository : AnsiDatabaseRepository
    {
        private string connectionString = "";
        private string user;
        private string password;

        public MySqlDatabaseRepository(string connectionString, string filePath):this(connectionString, filePath, "_migrations")
        {
        }
        public MySqlDatabaseRepository (string connectionString, string filePath, string tableName)
        {
            this.filePath = filePath;
            this.tableName = tableName;
            this.delimiter = ";\n";
            var pattern = new Regex("(.*?)(UID|User Id)=(.*?);\\s*?(Password|Pwd)=(.*?)(;|$)", RegexOptions.IgnoreCase);
            if (pattern.IsMatch(connectionString))
            {
                var matches = pattern.Matches(connectionString);
                user = matches[0].Groups[3].Value;
                password = matches[0].Groups[5].Value;
            }else
            {
                throw new Exception("Error parsing connection string using pattern: " + "(.*?)(UID|User Id)=(.*?);\\s*?(Password|Pwd)=(.*?)(;|$)");
            }
            this.connectionString = connectionString;
            this.connection = new MySqlConnection(connectionString);
        }


        public override MigrationTracker Init()
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


        public override IDbConnection GetConnWithoutDatabaseSpecified()
        {
            var systemConnection = new MySqlConnection(connectionString);
            systemConnection.Open();            
            systemConnection.ChangeDatabase("mysql");
            return systemConnection;
        }

        public override void CreateDatabaseIfNotExists()
        {
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = string.Format("create database if not exists {0}", DatabaseName);
            cmd.ExecuteNonQuery();
            connection.Close();            
        }

        protected override IDbCommand GetMigrationCmd(Migration migration)
        {
            var migrationCmd = (MySqlCommand)connection.CreateCommand();
            migrationCmd.CommandText = string.Format("insert into {0} VALUES(?key, ?hash)", tableName);
            migrationCmd.Parameters.Add("?key", migration.Version);
            migrationCmd.Parameters.Add("?hash", migration.Hash);
            return migrationCmd;
        }

        public override bool MigrationTableExists()
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

        public override string GenerateSqlScript(bool includeData)
        {
            var proc = new Process();
                var info = new ProcessStartInfo("mysqldump");
                var args = (!includeData ? "--no-data " : "") + "-u" + user + " -p" + password + " " + connection.Database;
                info.Arguments = args;
                info.UseShellExecute = false;
                info.RedirectStandardInput = true;
                info.RedirectStandardOutput = true;
                proc.StartInfo = info;
                proc.Start();
                var sql = proc.StandardOutput.ReadToEnd();
                return sql;    
       }

    }
}
