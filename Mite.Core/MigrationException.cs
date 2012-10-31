using System;

namespace Mite.Core
{
    [Serializable]
    public class MigrationException: Exception
    {
        private readonly Migration migration;
        private readonly MigrationDirection direction;
        private readonly Exception ex;

        public MigrationException(Migration migration, MigrationDirection direction,  Exception ex)
        {
            this.migration = migration;
            this.direction = direction;
            this.ex = ex;
        }

        public Exception Exception { get { return ex; } }
        public Migration Migration { get { return migration; } }
        public MigrationDirection Direction { get { return direction; } }
    }
}