using System;
using System.Collections.Generic;
using System.Linq;

namespace Mite.Core
{
    public class Migrator
    {
        private IMiteDatabase database;
        private readonly IDatabaseRepository databaseRepository;

        public Migrator(IMiteDatabase database, IDatabaseRepository databaseRepository)
        {
            this.database = database;
            this.databaseRepository = databaseRepository;
        }

        public MigrationResult StepUp()
        {
            if (database.IsValidState())
            {
                var version = this.database.Version;
                var firstMigration = this.database.UnexcutedMigrations.FirstOrDefault();
                if (firstMigration == null)
                {
                    return new MigrationResult(true, "No unexecuted migrations.", version, version);
                }
                databaseRepository.ExecuteUp(firstMigration);
                return new MigrationResult(true, version, firstMigration.Version);
            }
            else
            {
                throw new Exception("Database must be in a valid state before executing a StepUp");
            }
        }
        public MigrationResult StepDown()
        {
            if (!database.IsValidState())
                throw new Exception("Database must be in a valid state before executing a StepDown");
            var key = this.database.Version;
            IDictionary<string, Migration> migrationDictionary = this.database.GetMigrationDictionary();
            var migration = migrationDictionary[key];
            var resultingVersion =
                migrationDictionary.Keys.Where(x => x.CompareTo(key) < 0).OrderByDescending(x => x).FirstOrDefault();
            var resultingMigration = migrationDictionary[resultingVersion];
            if (migration == null)
            {
                return new MigrationResult(true, "No unexecuted migrations.", migration.Version, migration.Version);
            }
                
            databaseRepository.ExecuteDown(migration);
            return new MigrationResult(true, key, resultingMigration.Version);
        }

        public void FromScratch()
        {
            //drop the database and execute all the ups 
            if (databaseRepository.DatabaseExists())
            {
                databaseRepository.DropDatabase();
            }
            databaseRepository.CreateDatabase();
            database =databaseRepository.Init();
            foreach (var mig in database.UnexcutedMigrations)
            {
                databaseRepository.ExecuteUp(mig);
            }
        }
        public MigrationResult SafeResolution()
        {
            var priorVersion = database.Version;
            var lastValidMigrationVersion = database.LastValidMigration  == null ?  "" : database.LastValidMigration.Version;
            MigrateTo(lastValidMigrationVersion);
            database = databaseRepository.Create();
            Update();
            database = databaseRepository.Create();
            return new MigrationResult(true,priorVersion, database.Version ); 
        }

        public MigrationResult DirtyResolution()
        {
            //run all non-executed transactions
            var version = database.Version;
            foreach (var mig in database.UnexcutedMigrations)
            {
                databaseRepository.ExecuteUp(mig);
            }
            database = databaseRepository.Create();
            return new MigrationResult(true, version, database.Version);
        }
        public MigrationResult Update()
        {
            if (!database.IsValidState())
                throw new Exception("Database must be in a valid state in order to update it.");
            if (!databaseRepository.DatabaseExists())
                databaseRepository.Init();
            var version = database.Version;
            foreach (var mig in database.UnexcutedMigrations)
            {
                databaseRepository.ExecuteUp(mig);
            }
            database = databaseRepository.Create();
            return new MigrationResult(true, "", version, database.Version);
        }

        public MigrationResult MigrateTo(string destinationVersion)
        {
            
            string originalVersion = database.Version;
            bool isUp = database.Version.CompareTo(destinationVersion) < 0;
            if (isUp && !database.IsValidState())
                throw new Exception(
                    "Database must be in a valid state in order to use migrate in the up direction.  Try mite update instead.");
            if (!databaseRepository.MigrationTableExists())
                databaseRepository.Init();
            if (isUp)
            {
                var migrationsToExecute =
                    database.UnexcutedMigrations.Where(x => x.Version.CompareTo(destinationVersion) <= 0);
                foreach (var mig in migrationsToExecute)
                {
                    databaseRepository.ExecuteUp(mig);
                }
            }
            else
            {
                var migrationsToExecute =
                    database.ExecutedMigrations.Where(x => x.Version.CompareTo(destinationVersion) > 0);
                foreach (var mig in migrationsToExecute)
                {
                    databaseRepository.ExecuteDown(mig);
                }
            }

            this.database = databaseRepository.Create();
            return new MigrationResult(true, "", originalVersion, database.Version);
        }
    }
}
