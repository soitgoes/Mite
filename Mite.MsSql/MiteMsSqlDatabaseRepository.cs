using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mite.Core;

namespace Mite.MsSql
{
    public class MiteMsSqlDatabaseRepository: IMiteDatabaseRepository
    {
        private readonly string connectionString;
        private readonly string tableName;
        public MiteMsSqlDatabaseRepository(string connectionString):this(connectionString, "_migrations")
        {            
        }
        public MiteMsSqlDatabaseRepository(string connectionString, string tableName)
        {
            this.connectionString = connectionString;
            this.tableName = tableName;
        }

        public MiteDatabase Create()
        {
            //todo: get all the hashes from _migrations table
            
            return new MiteDatabase(null,null);
        }

        public void Save()
        {
            //todo: persist back to _migrations table
            throw new NotImplementedException();
        }
    }
}
