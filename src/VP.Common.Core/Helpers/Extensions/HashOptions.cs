using System.Collections.Generic;

namespace VP.Common.Core
{
    public class HashOptions
    {
        public HashOptions()
        {
            CharsToReplace = new List<KeyValuePair<char, char>>();
        }

        public List<KeyValuePair<char, char>> CharsToReplace { get; set; } 
    }
}