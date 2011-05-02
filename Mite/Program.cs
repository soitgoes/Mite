using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Mite
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args[0] =="-v")
            {
               Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Version); 
            }
            if (args[0] == "-c")
            {
                CreateMigration();
            }
            if (args.Length % 2 != 0)
            {
                Console.WriteLine("Invalid args specified.  Type 'mite /?' for a list of valid options.");
                return;
            }
            var options = GetOptions(args);
            Migrator.Migrate(options);
            Console.ReadLine();
        }
     
        private static void CreateMigration()
        {
            //open up notepad with the named migration as the name.
            var executingDirectory = Environment.CurrentDirectory; //todo: ensure this is correct when executed.
            var now = DateTime.Now;
            var baseName = now.ToString("yyyy-MM-dd") + "T" + now.ToString("hh-mm-ss") + "Z";
            var fileName = baseName +"-up.sql";
            var fullPath = executingDirectory +"\\" + fileName;
            File.WriteAllText(fileName, "/* put your UP sql schema migration script here and then save.*/");
            Console.WriteLine("Creating file '{0}'", fullPath);
            Process.Start(fullPath);
        }

        private static ConsoleOptions GetOptions(string[] args)
        {
            ConsoleOptions options = new ConsoleOptions();
            for (int i=0; i < args.Length; i+=2)
            {
                options[args[i]] = args[i + 1];
            }
            return options;
        }
    }
}
