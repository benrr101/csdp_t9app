////////////////////////////////////////////////////////////////////////////
// Model for Predictive Handling
// 
// Description: This class contains all the logic for finding matches in the
//              dictionary based the keys pressed. This uses a regular expression
//              match combined with LINQ to generate matches. Since it is
//              lazy-loading it is pretty performant. The match return
//              behavior is based on enumerators.
// Author: Benjamin Russell (brr1922)
////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using T9App2.Properties;

namespace T9App2
{
    class PredictiveHandler
    {
        /// <summary>
        /// The list of dictionary words
        /// </summary>
        private readonly List<string> _wordList;

        /// <summary>
        /// Enumeration over the list of words that match the regex
        /// </summary>
        private IEnumerator<string> _enumerator; 

        /// <summary>
        /// The matches from the word list that match the regex
        /// </summary>
        private IEnumerable<string> _matches; 

        /// <summary>
        /// The regex to find matches from
        /// </summary>
        private string _regex;
        public string Regex
        {
            get { return _regex; }
            set
            {
                _regex = value;

                if (_regex != null)
                {
                    // Recalculate the regex matches
                    Regex regex = new Regex("^" + Regex + "$");
                    _matches = _wordList.Where(word => regex.IsMatch(word));
                    _enumerator = _matches.GetEnumerator();
                }
                else
                {
                    // Clear the matches
                    _matches = null;
                }
            }
        }

        /// <summary>
        /// Constructor for the predictive handler. Loads the dictionary
        /// of words into a list of words.
        /// </summary>
        public PredictiveHandler()
        {
            _wordList = new List<string>();

            // Grab the location of the dictionary from the properties
            string dictLocation = Settings.Default.DictionaryLocation;
            FileStream dictFile = File.OpenRead(dictLocation);
            StreamReader dictStream = new StreamReader(dictFile);

            // Start parsing the dictionary into a list of words
            string line;
            while((line = dictStream.ReadLine()) != null)
            {
                _wordList.Add(line.Trim().ToLower());
            }
        }

        /// <summary>
        /// Removes the last character class from the regular expression
        /// </summary>
        public void DropLastRegex()
        {
            // Drop the last character class from the regular expression
            int lastOpenBracket = Regex.LastIndexOf('[');
            Regex = Regex.Substring(0, lastOpenBracket);
        }

        /// <summary>
        /// Return the next value from the matches iterator. If no matches exist
        /// return a line of dashes. The iterator will loop.
        /// </summary>
        /// <returns>The next match if there are matches, a line of dashes otherwise</returns>
        public string NextMatch()
        {
            // Make sure we have matches to iterate over
            if (!_matches.Any())
            {
                // Return dashes
                int dashCount = Regex.Split('[').Length - 1;
                string dashes = String.Empty;
                for (int i = 0; i < dashCount; i++)
                {
                    dashes += '-';
                }
                return dashes;
            }

            // Make sure we have an enumerator
            if (_enumerator == null)
                _enumerator = _matches.GetEnumerator();

            // Reset the loop if we're at the end
            if (!_enumerator.MoveNext())
            {
                _enumerator = _matches.GetEnumerator();
                _enumerator.MoveNext();
            }

            return _enumerator.Current;
        }
    }
}
