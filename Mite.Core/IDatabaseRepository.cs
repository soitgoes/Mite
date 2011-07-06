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
    }
}