using System;
using System.Collections.Generic;

namespace Mite
{
    public class Migration : IComparable
    {
        public string Version { get; set; }
        public string Sql { get; set; }
        public int CompareTo(object obj)
        {
            return ((Migration) obj).Version.CompareTo(this.Version);
        }
    }
    public class MigrationContainer
    {
        public void Add(string version, string migration)
        {
            
        }

        public List<Migration> GetScripts(string currentVersion, string destinationVersion)
        {
            return null;
        }
    }
}