using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Mite.Core
{
    public class Migrator
    {
        private IMiteDatabase database;
        private readonly IMiteDatabaseRepository databaseRepository;

        public Migrator(IMiteDatabase database, IMiteDatabaseRepository databaseRepository)
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
            databaseRepository.ExecuteDown(migration);
            return new MigrationResult(true, key, resultingMigration.Version);
        }

        public void FromScratch()
        {
            //delete execute all downs that are in the database  then execute the ups.
            foreach (var mig in database.ExecutedMigrations)
            {
                databaseRepository.ExecuteDown(mig);
            }
            foreach (var mig in database.GetMigrationDictionary().Values)
            {
                databaseRepository.ExecuteUp(mig);
            }
        }
        public MigrationResult SafeResolution()
        {
            var version = database.Version;
            MigrateTo(database.LastValidMigration.Version);
            Update();
            database = databaseRepository.Create();
            return new MigrationResult(true,version, database.Version );
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
            if (!database.IsValidState())
                throw new Exception(
                    "Database must be in a valid state in order to use migrate to.  Try mite update instead.");
            string originalVersion = database.Version;
            bool isUp = database.Version.CompareTo(destinationVersion) > 0;
            Migration lastMigration = null;
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
