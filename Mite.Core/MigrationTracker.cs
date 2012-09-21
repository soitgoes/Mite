using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Mite.Core
{
    /// <summary>
    /// Item which computes which scripts have been run and not run
    /// </summary>
    public class MigrationTracker : IMigrationTracker
    {
        private readonly IList<Migration> migrations;
        private readonly IDictionary<string, string> hashes;

        public MigrationTracker(IEnumerable<Migration> migrations, IDictionary<string, string> hashes)
        {
            this.migrations = migrations.OrderBy(x => x.Version).ToList();
            this.hashes = hashes ?? new Dictionary<string, string>();
        }
        public IEnumerable<Migration> MigrationsSince(DateTime dateTime)
        {
            return UnexcutedMigrations.Where(x => x.Version.CompareTo(dateTime.ToIso()) > 0);
        }
        public IEnumerable<Migration> UnexcutedMigrations
        {
            get { return migrations.Where(x => !hashes.ContainsKey(x.Version)).OrderBy(x => x.Version); }
        }
        public bool IsHashMismatch()
        {
            return migrations.Where(mig => hashes.ContainsKey(mig.Version)).Any(mig => mig.Hash != hashes[mig.Version]);
        }

        public IDictionary<string , Migration> GetMigrationDictionary()
        {
            return migrations.ToDictionary(mig => mig.Version);
        }
        public bool IsMigrationGap()
        {
            var latestDatabaseVersion = hashes.Keys.OrderByDescending(x => x).FirstOrDefault();
            if (latestDatabaseVersion == null)
                return false;
            var itemsToTest  =migrations.Where(x => x.Version.CompareTo(latestDatabaseVersion) < 0).OrderBy(x => x.Version);
            return itemsToTest.Any(mig => !hashes.Keys.Contains(mig.Version));
        }

        public bool IsValidState()
        {
            return !IsMigrationGap() && !IsHashMismatch();
        }

        public bool CanGoUp { get { return IsValidState() && this.UnexcutedMigrations.Count() > 0; } }
        public bool CanGoDown { get { return IsValidState() && this.UnexcutedMigrations.Count() != this.migrations.Count(); } }

        public Migration LastValidMigration
        {
            get
            {
                Migration lastValidMigration = null;
                foreach (var mig in migrations)
                {
                    if (hashes.ContainsKey(mig.Version) && hashes[mig.Version] == mig.Hash)
                    {
                        lastValidMigration = mig;
                        
                    }else
                    {
                        return lastValidMigration;
                    }
                   
                }
                return lastValidMigration;
            }
        }

        public IEnumerable<Migration> Migrations
        {
            get { return this.migrations; }
        }

        public IEnumerable<Migration> InvalidMigrations()
        {
            return from mig in migrations where hashes.ContainsKey(mig.Version) let hash = hashes[mig.Version] where hash != mig.Hash select mig;
        }

        public string Version
        {
            get {
                if (hashes.Count == 0)
                    return "0";
                var lastKey = hashes.Keys.LastOrDefault();
            if (lastKey == null)
                return string.Empty;
                return lastKey;
            }
        }

        public IEnumerable<Migration> ExecutedMigrations
        {
            get { return migrations.Where(x => hashes.ContainsKey(x.Version)).OrderByDescending(x => x.Version); }
        }
    }
}