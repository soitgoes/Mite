using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Mite.Core
{
    /// <summary>
    /// Item which computes which scripts have been run and not run
    /// </summary>
    public class MiteDatabase : IMiteDatabase
    {
        private readonly IList<Migration> migrations;
        private readonly IDictionary<string, string> hashes;

        public MiteDatabase(IEnumerable<Migration> migrations, IDictionary<string, string> hashes)
        {
            this.migrations = migrations.OrderBy(x => x.Version).ToList();
            this.hashes = hashes ?? new Dictionary<string, string>();
        }
        public IEnumerable<Migration> MigrationsSince(DateTime dateTime)
        {
            return UnexcutedMigrations.Where(x => x.Version.CompareTo(DateTimeHelper.ToIso(dateTime)) > 0);
        }
        public IEnumerable<Migration> UnexcutedMigrations
        {
            get { return migrations.Where(x => !hashes.ContainsKey(x.Version)).OrderBy(x => x.Version); }
        }
        public bool IsHashMismatch()
        {

            foreach (var mig in migrations)
            {
                if (hashes.ContainsKey(mig.Version))
                {
                    if (mig.Hash != hashes[mig.Version])
                        return true;    
                }
            }
            return false;
        }
        public IDictionary<string , Migration> GetMigrationDictionary()
        {
            var dict = new Dictionary<string, Migration>();
            foreach (var mig in this.migrations)
            {
                dict.Add(mig.Version, mig);
            }
            return dict;
        }
        public bool IsMigrationGap()
        {
            var latestDatabaseVersion = hashes.Keys.OrderByDescending(x => x).FirstOrDefault();
            if (latestDatabaseVersion == null)
                return false;
            var itemsToTest  =migrations.Where(x => x.Version.CompareTo(latestDatabaseVersion) < 0).OrderBy(x => x.Version);
            foreach(var mig in itemsToTest)
            {
                if (!hashes.Keys.Contains(mig.Version))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsValidState()
        {
            return !IsMigrationGap() && !IsHashMismatch();
        } 

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
            foreach (var mig in migrations)
            {
                if (hashes.ContainsKey(mig.Version))
                {
                    var hash = hashes[mig.Version];
                    if (hash != mig.Hash)
                        yield return mig;    
                }
            }
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