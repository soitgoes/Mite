using System.Collections.Generic;
using System.Linq;

namespace Mite.Core
{
    public class MigrationContainer : List<Migration>
    {
        public MigrationContainer(params Migration[] migrations)
        {
            this.AddRange(migrations);
        }
        public MigrationPlan GetMigrationPlan(string currentVersion, string destinationVersion)
        {
            var plan = new MigrationPlan();
            currentVersion = string.IsNullOrEmpty(currentVersion) ? "0" : currentVersion;
            plan.OriginVersion = currentVersion;
            var greatestVersion = this.OrderByDescending(x => x.Version).FirstOrDefault().Version;
            plan.DestinationVersion = greatestVersion.CompareTo(destinationVersion) < 0 ? greatestVersion : destinationVersion; //TODO: change if greater than all 
            var direction = currentVersion.CompareTo(destinationVersion) > 0 ? MigrationType.Down : MigrationType.Up;
            if (direction == MigrationType.Up)
            {
                //get all the scripts greater than the currentversion and less than or equal to the destination that are of type up
                plan.SqlToExecute =
                    this.Where(
                        x => x.Type == MigrationType.Up && (x.Version.CompareTo(currentVersion) > 0 && x.Version.CompareTo(destinationVersion) <= 0)).OrderBy(x =>x.Version).
                        Select(x => x.Sql).ToArray();                
            }else
            {
                //get all the down scripts equal to or less than the origin and greater than the destination\
                plan.SqlToExecute =
                    this.Where(
                        x =>
                        x.Type == MigrationType.Down &&
                        (x.Version.CompareTo(currentVersion) <= 0 && x.Version.CompareTo(destinationVersion) > 0)).
                        OrderByDescending(x => x.Version).Select(x => x.Sql).ToArray();
            }

            return plan;
        }
        
    }
    public class MigrationPlan
    {
        public string[] SqlToExecute { get; set; }
        public string OriginVersion { get; set; }
        public string DestinationVersion { get; set; }
    }
}