using System;
using System.Data;

namespace Mite.Core
{
    public interface  IDatabaseRepository :IDisposable
    {
        MigrationTracker Init();
        MigrationTracker Create();
        bool DatabaseExists();
        void CreateDatabaseIfNotExists();
        /// <summary>
        /// This method executes the sql for the migration and records the record in the migrations table (it is not responsible for whether or not to execute the migration).
        /// </summary>
        /// <param name="migration"></param>
        MigrationTracker ExecuteUp(Migration migration);
        MigrationTracker ExecuteDown(Migration migration);
        string DatabaseName { get; set; }
        IDbConnection Connection { get; set; }
        bool CheckConnection();
        void DropMigrationTable();
        bool MigrationTableExists();
        string GenerateSqlScript(bool includeData);

        /// <summary>
        /// Record Migration should only be used if you are genning the Migration in the init
        /// </summary>
        /// <param name="migration"></param>
        /// <returns></returns>
        MigrationTracker RecordMigration(Migration migration);

        void DropDatabase();
        IDbConnection GetConnWithoutDatabaseSpecified();
        void WriteHash(Migration migration);
        void ForceWriteMigration(Migration migration);
    }
}