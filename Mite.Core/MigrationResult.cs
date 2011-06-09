namespace Mite.Core
{
    public class MigrationResult
    {
        private readonly bool success;
        private readonly string message;
        private readonly string priorToMigration;
        private readonly string afterMigration;

        public MigrationResult(bool success, string message, string priorToMigration, string afterMigration)
        {
            this.success = success;
            this.message = message;
            this.priorToMigration = priorToMigration;
            this.afterMigration = afterMigration;
        }
        public MigrationResult(bool success, string message)
        {
            this.success = success;
            this.message = message;
        }   
        public MigrationResult(bool success, string priorToMigration, string afterMigration)
        {
            this.success = success;
            this.priorToMigration = priorToMigration;
            this.afterMigration = afterMigration;
            this.message = string.Format("Migration from {0} to {1} successful", priorToMigration, afterMigration);
        }

        public bool Success { get { return success; } }
        public string Message { get { return message; } }
        public string PriorToMigration { get { return priorToMigration; } }
        public string AfterMigration { get { return afterMigration; } }
    }
}