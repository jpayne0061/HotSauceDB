using SharpDb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpDb.Services.Parsers
{
    public class SelectParser : GeneralParser
    {
        public IList<string> GetColumns(string query)
        {
            int startIndex = 6;

            int endIndex = query.IndexOf("from") - 1;

            string columns = "";

            for (int i = startIndex; i < endIndex; i++)
            {
                columns += query[i];
            }

            columns = Regex.Replace(columns, @"\s+", "");

            List<string> columnsSplit = columns.Split(',').ToList();

            return columnsSplit;
        }

        public string GetTableName(string query)
        {
            int startIndex = query.ToLower().IndexOf("from") + 4;

            query = query.Substring(startIndex);

            query = query.TrimStart();

            string tableName = "";

            for (int i = 0; i < query.Length; i++)
            {
                if(query[i] == ' ')
                    break;

                tableName += query[i];
            }

            return tableName;
        }

        public int IndexOfWhereClause(string query, string tableName)
        {
            List<string> queryParts = query.Split(' ')
                .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

            int tableNameIndex = queryParts.IndexOf(tableName);

            if(queryParts.Count() > tableNameIndex + 1
                && queryParts[tableNameIndex + 1].ToLower() == "where")
            {
                return tableNameIndex + 1;
            }

            return -1;
        }

        public List<string> ParsePredicates(string query)
        {
            var predicates = new List<string>();

            //https://stackoverflow.com/questions/14655023/split-a-string-that-has-white-spaces-unless-they-are-enclosed-within-quotes
            //https://stackoverflow.com/users/1284526/c%c3%a9dric-bignon
            var queryParts = query.Split("'")
             .Select((element, index) => index % 2 == 0  // If even index
                                   ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)  // Split the item
                                   : new string[] { "'" + element + "'" })  // Keep the entire item
             .SelectMany(element => element).Select(x => x.Replace("\r\n", "")).ToList();

            var whereClauseIndex = IndexOfWhereClause(query, GetTableName(query));

            string firstPredicate = queryParts[whereClauseIndex + 0] + " " + 
                                    queryParts[whereClauseIndex + 1] + " " +
                                    queryParts[whereClauseIndex + 2] + " " +
                                    queryParts[whereClauseIndex + 3];

            predicates.Add(firstPredicate);

            int operatorIndex = whereClauseIndex + 4;

            while (operatorIndex < queryParts.Count())
            {
                string currentPredicate = queryParts[operatorIndex + 0] + " " +
                                          queryParts[operatorIndex + 1] + " " +
                                          queryParts[operatorIndex + 2] + " " +
                                          queryParts[operatorIndex + 3];

                predicates.Add(currentPredicate);

                operatorIndex += 4;
            }

            return predicates;
        }

    }
}
