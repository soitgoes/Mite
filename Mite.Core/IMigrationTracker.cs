using System;
using System.Collections;
using System.Collections.Generic;

namespace Mite.Core
{
    /// <summary>
    /// Determines state of the database given provided migrations
    /// </summary>
    public interface IMigrationTracker
    {
        IEnumerable<Migration> MigrationsSince(DateTime dateTime);
        IEnumerable<Migration> UnexcutedMigrations { get; }
        bool IsHashMismatch();
        bool IsMigrationGap();
        bool IsValidState();
        bool CanGoUp { get; }
        bool CanGoDown { get; }
        
        Migration LastValidMigration { get; }
        IEnumerable<Migration> Migrations { get; }
        IEnumerable<Migration> InvalidMigrations();
        string Version { get; }
        IEnumerable<Migration> ExecutedMigrations { get; }
        IDictionary<string , Migration> GetMigrationDictionary();
    }
}