using System;

namespace Mite.Core
{
    public interface  IDatabaseRepository :IDisposable
    {
        MiteDatabase Init();
        MiteDatabase Create();
        /// <summary>
        /// This method executes the sql for the migration and records the record in the migrations table (it is not responsible for whether or not to execute the migration).
        /// </summary>
        /// <param name="migration"></param>
        MiteDatabase ExecuteUp(Migration migration);
        MiteDatabase ExecuteDown(Migration migration);
        bool CheckConnection();
        void DropMigrationTable();
        bool MigrationTableExists();
        string GenerateSqlScript(bool includeData);
        /// <summary>
        /// Execute script does not create an entry in the _migrations table.  It's reserved for _base.sql
        /// </summary>
        /// <param name="sql"></param>
        void ExecuteScript(string sql);
    }
}