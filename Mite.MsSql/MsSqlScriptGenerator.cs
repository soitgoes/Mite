using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mite.Core;
using Microsoft.SqlServer.Management.Smo;

namespace Mite.MsSql
{
    public class MsSqlScriptGenerator : IScriptGenerator
    {
        public void GenerateScript(TextWriter writer, bool includeData)
        {
            
        }

        public void GenerateScript(TextWriter writer)
        {
            GenerateScript(writer, false);
        }
    }
}
