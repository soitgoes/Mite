namespace Mite.Core
{
    /// <summary>
    /// Successful migration is assumed since MigrationException is thrown in the event of a failure
    /// </summary>
    public class MigrationResult
    {
        private readonly string message;
        private readonly string priorToMigration;
        private readonly string afterMigration;

        public MigrationResult(string message, string priorToMigration, string afterMigration)
        {
            this.message = message;
            this.priorToMigration = priorToMigration;
            this.afterMigration = afterMigration;
        }
        public MigrationResult( string message)
        {
            this.message = message;
        }   
        public MigrationResult(string priorToMigration, string afterMigration)
        {
            this.priorToMigration = priorToMigration;
            this.afterMigration = afterMigration;
            this.message = string.Format("Migration from {0} to {1} successful", priorToMigration, afterMigration);
        }

        public string Message { get { return message; } }
        public string PriorToMigration { get { return priorToMigration; } }
        public string AfterMigration { get { return afterMigration; } }
    }
}