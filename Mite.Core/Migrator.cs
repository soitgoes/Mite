using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Mite.Core
{
    public class Migrator
    {
        private const string verifyDatabaseName = "MiteVerify";
        private IMigrationTracker tracker;
        private readonly IDatabaseRepository databaseRepository;

        public Migrator(IMigrationTracker tracker, IDatabaseRepository databaseRepository)
        {
            this.tracker = tracker;
            this.databaseRepository = databaseRepository;
        }
        public IMigrationTracker Tracker { get { return tracker; } }
        public IDatabaseRepository DatabaseRepository { get { return databaseRepository; } }
        public IDbConnection Connection { get { return databaseRepository.Connection; } }

        
        public MigrationResult StepUp()
        {
            if (tracker.IsValidState())
            {
                var version = tracker.Version;
                var firstMigration = tracker.UnexcutedMigrations.FirstOrDefault();
                if (firstMigration == null)
                {
                    return new MigrationResult( "No unexecuted migrations.", version, version);
                }
                databaseRepository.ExecuteUp(firstMigration);
                return new MigrationResult( version, firstMigration.Version);
            }
            else
            {
                throw new Exception("Database must be in a valid state before executing a StepUp");
            }
        }
        public MigrationResult StepDown()
        {
            if (!tracker.IsValidState())
                throw new Exception("Database must be in a valid state before executing a StepDown");
            var key = tracker.Version;
            IDictionary<string, Migration> migrationDictionary = tracker.GetMigrationDictionary();
            var migration = migrationDictionary[key];
            var resultingVersion =
                migrationDictionary.Keys.Where(x => x.CompareTo(key) < 0).OrderByDescending(x => x).FirstOrDefault();
            var resultingMigration = migrationDictionary[resultingVersion];
            if (migration == null)
            {
                return new MigrationResult( "No unexecuted migrations.", Tracker.Version, Tracker.Version);
            }
                
            databaseRepository.ExecuteDown(migration);
            return new MigrationResult( key, resultingMigration.Version);
        }
        

        public void FromScratch()
        {
            //drop the database and execute all the ups 
            if (databaseRepository.DatabaseExists())
            {
                databaseRepository.DropDatabase();
            }
            databaseRepository.CreateDatabaseIfNotExists();
            tracker =databaseRepository.Init();
            foreach (var mig in tracker.UnexcutedMigrations)
            {
                databaseRepository.ExecuteUp(mig);
            }
        }   
        /// <summary>
        /// In a temporary database migrate all the way up then back down
        /// </summary>
        /// <returns></returns>
        public bool Verify()
        {
            var storeDbName = databaseRepository.Connection.Database;
            //drop and recreate MiteVerify
            databaseRepository.Connection.Open();
            databaseRepository.Connection.ChangeDatabase(verifyDatabaseName);
            databaseRepository.Connection.Close();
            databaseRepository.DropDatabase();
            databaseRepository.CreateDatabaseIfNotExists();
            var verifier = new Migrator(this.tracker, databaseRepository);
            try
            {
                var cnt = verifier.Tracker.Migrations.Count();
                for (var i = 0; i < cnt; i++)
                    verifier.StepUp();
                for (var i = 0; i < cnt; i++)
                    verifier.StepDown();
                return true;
            }finally
            {
                databaseRepository.Connection.ChangeDatabase(storeDbName);
            }
        }

        public MigrationResult SafeResolution()
        {
            var priorVersion = tracker.Version;
            var lastValidMigrationVersion = tracker.LastValidMigration  == null ?  "" : tracker.LastValidMigration.Version;
            MigrateTo(lastValidMigrationVersion);
            tracker = databaseRepository.Create();
            Update();
            tracker = databaseRepository.Create();
            return new MigrationResult(priorVersion, tracker.Version ); 
        }

        public MigrationResult DirtyResolution()
        {
            //run all non-executed transactions
            var version = tracker.Version;
            foreach (var mig in tracker.UnexcutedMigrations)
            {
                databaseRepository.ExecuteUp(mig);
            }
            tracker = databaseRepository.Create();
            return new MigrationResult( version, tracker.Version);
        }
        public MigrationResult Update()
        {
            if (!tracker.IsValidState())
                throw new Exception("Database must be in a valid state in order to update it.");
            if (!databaseRepository.DatabaseExists())
                databaseRepository.Init();
            var version = tracker.Version;
            foreach (var mig in tracker.UnexcutedMigrations)
            {
                databaseRepository.ExecuteUp(mig);
            }
            tracker = databaseRepository.Create();
            return new MigrationResult( "", version, tracker.Version);
        }

        public MigrationResult MigrateTo(string destinationVersion)
        {
            
            string originalVersion = tracker.Version;
            bool isUp = tracker.Version.CompareTo(destinationVersion) < 0;
            if (isUp && !tracker.IsValidState())
                throw new Exception(
                    "Database must be in a valid state in order to use migrate in the up direction.  Try mite update instead.");
            if (!databaseRepository.MigrationTableExists())
                databaseRepository.Init();
            if (isUp)
            {
                var migrationsToExecute =
                    tracker.UnexcutedMigrations.Where(x => x.Version.CompareTo(destinationVersion) <= 0);
                foreach (var mig in migrationsToExecute)
                {
                    databaseRepository.ExecuteUp(mig);
                }
            }
            else
            {
                var migrationsToExecute =
                    tracker.ExecutedMigrations.Where(x => x.Version.CompareTo(destinationVersion) > 0);
                foreach (var mig in migrationsToExecute)
                {
                    databaseRepository.ExecuteDown(mig);
                }
            }

            tracker = databaseRepository.Create();
            return new MigrationResult( "", originalVersion, tracker.Version);
        }
    }
}
