using System;

namespace Mite.Core
{
    public interface  IDatabaseRepository :IDisposable
    {
        MiteDatabase Init();
        MiteDatabase Create();
        bool DatabaseExists();
        void CreateDatabase();
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
        /// Record Migration should only be used if you are genning the Migration in the init
        /// </summary>
        /// <param name="migration"></param>
        /// <returns></returns>
        MiteDatabase RecordMigration(Migration migration);

        void DropDatabase();
    }
}