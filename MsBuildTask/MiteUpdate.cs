using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mite.Builder;

namespace MsBuildTask {

    /// <summary>
    /// MiteUpdate brings the database up to the most recent schema
    /// </summary>
    public class MiteUpdate : Task {
        public override bool Execute() {
            try {
                var migrator = MigratorFactory.GetMigrator(ScriptsDirectory);
                var result = migrator.Update();
                Log.LogMessage("Success: " + result.PriorToMigration +" to " + result.AfterMigration);
                return true;
            } catch (Exception ex) {
                Log.LogError(ex.Message);
                return false;
            }
        }
        [Required]
        public string ScriptsDirectory { get; set; }
    }
}
