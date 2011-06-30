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
        private readonly List<Migration> migrationFiles;
        private readonly string[] hashes;

        public MiteDatabase(List<Migration> migrationFiles, string[] hashes)
        {
            this.migrationFiles = migrationFiles;
            this.hashes = hashes;
        }
        public IEnumerable<Migration> MigrationsSince(DateTime dateTime)
        {
            return UnexcutedMigrations().Where(x => x.Version.CompareTo(dateTime.ToIso()) > 0);
        }
        public IEnumerable<Migration> UnexcutedMigrations()
        {
            return migrationFiles.Where(x => !hashes.Contains(x.Hash)).OrderBy(x => x.Version);
        }
    }
}