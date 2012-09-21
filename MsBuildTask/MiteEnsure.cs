using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mite.Builder;

namespace MsBuildTask {

    /// <summary>
    /// Mite Ensure verifies that all of the checksums on migrations match what is in the database
    /// Returns false if they do not.
    /// </summary>
    public class MiteEnsure : Task {
        public override bool Execute() {
            var migrator = MigratorFactory.GetMigrator(ScriptsDirectory);
            var result = migrator.Tracker.IsValidState();
            if (result)
            {
                Log.LogMessage("Mite database status is good.");
            }else
            {
                Log.LogError("Mite status is not clean");
                foreach (var migration in migrator.Tracker.InvalidMigrations())
                {
                    Log.LogError("Mismatch checksum on: " + migration.Version +"[" + migration.Hash+ "]");
                }
            }
            return result;
        }

        [Required]
        public string ScriptsDirectory { get; set; }
    }
}