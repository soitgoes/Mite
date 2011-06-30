using System;
using System.Security.Cryptography;
using System.Text;

namespace Mite.Core
{
    public class Migration : IComparable
    {
        private readonly string version;
        private readonly MigrationType type;
        private readonly string sql;
        private readonly string hash;

        public Migration(string version, MigrationType type, string sql)
        {
            this.version = version;
            this.type = type;
            this.sql = sql;
            var crypto = new SHA1CryptoServiceProvider();
            this.hash = Convert.ToBase64String(crypto.ComputeHash(Encoding.UTF8.GetBytes(sql))); 
        }
        public string Hash { get { return hash; } }
        public string Version { get { return version; } }
        public string Sql { get { return sql; } }
        public MigrationType Type { get { return this.type; } }
        public int CompareTo(object obj)
        {
            var comparable = (Migration) obj;
            if (comparable.Hash == this.Hash)
                return 0;
            return (comparable).Version.CompareTo(this.Version); //string comparison works fine for ISO8611
        }
    }
    public enum MigrationType {
        Up,
        Down
    }
}