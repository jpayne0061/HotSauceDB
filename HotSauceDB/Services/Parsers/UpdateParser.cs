using System;
using System.Collections.Generic;
using System.Linq;

namespace HotSauceDb.Services.Parsers
{
    public class UpdateParser : GeneralParser
    {
        public List<KeyValuePair<string, string>> GetUpdates(string query)
        {
            List<string> queryParts = SplitOnSeparatorsExceptQuotesAndParantheses(query, new char[] { ' ', '\r', '\n', ',' });

            for (int i = 0; i < queryParts.Count; i++)
            {
                if(IsNonEmptyStringValue(queryParts[i]))
                {
                    continue;
                }

                queryParts[i] = ToLowerAndTrim(queryParts[i]);
            }

            int whereClauseIndex = queryParts.IndexOf("where");

            int start = queryParts.IndexOf("set") + 1;

            List<string> setColumns = queryParts.GetRange(start, queryParts.Count - start).TakeWhile(x => x != "where").ToList();

            List<string> setValuesAndColumns = SplitOnSeparatorsExceptQuotesAndParantheses(string.Join(' ', setColumns), new char[] { '=', ' ', '\r', '\n', ',' });

            var valuePairs = new List<KeyValuePair<string, string>>();

            var currentKey = "";

            for (int i = 0; i < setValuesAndColumns.Count; i++)
            {
                if(i % 2 == 1)
                {
                    var kvp = new KeyValuePair<string, string>(currentKey, setValuesAndColumns[i]);

                    valuePairs.Add(kvp);
                }
                else
                {
                    currentKey = setValuesAndColumns[i];
                }
            }

            return valuePairs;
        }

        public string GetTableName(string query)
        {
            query = ToLowerAndTrim(query);

            List<string> parts = SplitOnSeparatorsExceptQuotesAndParantheses(query, new char[] { ' ', '\r', '\n' });

            return parts[1];
        }

        public KeyValuePair<string, IComparable> GetUpdates()
        {
            throw new NotImplementedException();
        }

        public bool HasPredicates()
        {
            throw new NotImplementedException();
        }

        private bool IsNonEmptyStringValue(string str)
        {
            if(string.IsNullOrWhiteSpace(str))
            {
                return false;
            }

            if(str.First() == '\'' && str.Last() == '\'')
            {
                return true;
            }

            return false;
        }
    }
}
