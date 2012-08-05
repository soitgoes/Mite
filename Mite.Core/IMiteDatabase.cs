using System;
using System.Collections;
using System.Collections.Generic;

namespace Mite.Core
{
    public interface IMiteDatabase
    {
        IEnumerable<Migration> MigrationsSince(DateTime dateTime);
        IEnumerable<Migration> UnexcutedMigrations { get; }
        bool IsHashMismatch();
        bool IsMigrationGap();
        bool IsValidState();
        Migration LastValidMigration { get; }
        IEnumerable<Migration> Migrations { get; }
        IEnumerable<Migration> InvalidMigrations();
        string Version { get; }
        IEnumerable<Migration> ExecutedMigrations { get; }
        IDictionary<string , Migration> GetMigrationDictionary();
    }
}