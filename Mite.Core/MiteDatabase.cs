using System;
using System.Collections.Generic;
using System.Linq;

namespace Mite.Core
{
    /// <summary>
    /// Item which computes which scripts have been run and not run
    /// </summary>
    public class MiteDatabase
    {
        private readonly IList<Migration> migrations;
        private readonly IDictionary<string, string> hashes;

        public MiteDatabase(IList<Migration> migrations, IDictionary<string, string> hashes)
        {
            this.migrations = migrations.OrderBy(x => x.Version).ToList();
            this.hashes = hashes;
        }
        public IEnumerable<Migration> MigrationsSince(DateTime dateTime)
        {
            return UnexcutedMigrations().Where(x => x.Version.CompareTo(dateTime.ToIso()) > 0);
        }
        public IEnumerable<Migration> UnexcutedMigrations()
        {
            return migrations.Where(x => !hashes.Values.Contains(x.Hash)).OrderBy(x => x.Version);
        }
        public bool IsHashMismatch()
        {

            foreach (var mig in migrations)
            {
                if (hashes.ContainsKey(mig.Version))
                {
                    if (mig.Hash != hashes[mig.Version])
                        return false;    
                }
            }
            return true;
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
            return !IsMigrationGap() && IsHashMismatch();
        }
        
        public IEnumerable<Migration> InvalidMigrations()
        {
            foreach (var mig in migrations)
            {
                var hash = hashes[mig.Version];
                if (hash != mig.Hash)
                    yield return mig;
            }
        }
    }
}