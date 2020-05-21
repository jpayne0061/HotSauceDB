using HotSauceDB.Statics;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpDb.Services.Parsers
{
    //need to get row locations of all rows that need updated
    //

    public class UpdateParser : GeneralParser
    {
        public List<string> GetColumns(string query)
        {
            query = ToLowerAndTrim(query);



            throw new NotImplementedException();
        }

        public string GetTableName(string query)
        {
            query = ToLowerAndTrim(query);

            List<string> parts = query.SplitOnWhiteSpace();

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

    }
}
