using System;

namespace Mite
{
    public class Migration : IComparable
    {
        private readonly string version;
        private readonly MigrationType type;
        private readonly string sql;

        public Migration(string version, MigrationType type, string sql)
        {
            this.version = version;
            this.type = type;
            this.sql = sql;
        }

        public string Version { get { return version; } }
        public string Sql { get { return sql; } }
        public MigrationType Type { get { return this.type; } }
        public int CompareTo(object obj)
        {
            return ((Migration) obj).Version.CompareTo(this.Version); //string comparison works fine for ISO8611
        }
    }
}