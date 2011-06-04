using System.Collections.Generic;

namespace Mite
{
    public class ConsoleOptions
    {
        private Dictionary<string, string> dictionary;
        
        public ConsoleOptions()
        {
            dictionary = new Dictionary<string, string>();
        }
        public string this[string key]
        {
            get
            {
                //TODO:  Check to see if the options contained and notify that the option is not set.
                if (dictionary.ContainsKey(key) && !string.IsNullOrEmpty(dictionary[key]))
                    return dictionary[key];
                //Console.WriteLine(string.Format("Options '{0}' has not been set", key));
                return "";
            }
            set
            {
                //TODO:  Check to see if the options is valid and then throw an exception if it is not.
                dictionary[key] = value;
            }
        }
        //TODO : handle invalid option gracefully
        public string PathToMigrationScripts { get { return this["-p"]; } }

        public string DestinationVersion { get
        {
            if (dictionary.ContainsKey("-d"))
            {
                return dictionary["-d"];
            }else
            {
                return null;
            }
        }}
    }
}