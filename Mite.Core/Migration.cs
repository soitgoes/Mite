using System;
using System.Security.Cryptography;
using System.Text;

namespace Mite.Core
{
    public class Migration : IComparable
    {
        private readonly string version;
        private readonly string upSql;
        private readonly string downSql;
        private readonly string hash;

        public Migration(string version, string upSql, string downSql)
        {
            this.version = version;
            this.upSql = upSql;
            this.downSql = downSql;
            var crypto = new SHA1CryptoServiceProvider();
            this.hash = Convert.ToBase64String(crypto.ComputeHash(Encoding.UTF8.GetBytes(upSql + downSql))); 
        }
        public string Hash { get { return hash; } }
        public string Version { get { return version; } }
        public string UpSql { get { return upSql; } }
        public string DownSql { get { return downSql; } }
        public int CompareTo(object obj)
        {
            var comparable = (Migration) obj;
            if (comparable.Hash == this.Hash)
                return 0;
            return (comparable).Version.CompareTo(this.Version); //string comparison works fine for ISO8611
        }
    }
}