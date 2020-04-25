using System;
using System.Collections.Generic;
using System.Text;

namespace SharpDb.Services.Parsers
{
    public class GeneralParser
    {
        private string PrepareQuery(string query)
        {
            query = query.Trim();

            query = query.ToLower();

            return query;
        }

        private string GetQueryType(string query)
        {
            query = PrepareQuery(query);

            return TruncateLongString(query, 6);
        }


        //https://stackoverflow.com/questions/3566830/what-method-in-the-string-class-returns-only-the-first-n-characters
        public string TruncateLongString(string str, int maxLength)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            return str.Substring(0, Math.Min(str.Length, maxLength));
        }
    }
}
