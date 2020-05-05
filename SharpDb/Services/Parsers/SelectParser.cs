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
            query = query.ToLower().Trim();

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

            return tableName.ToLower();
        }

        public int IndexOfWhereClause(string query, string tableName)
        {
            query = query.ToLower();

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
             .SelectMany(element => element).Select(x => x.Replace("\r\n", "")).
             Where(x => !string.IsNullOrWhiteSpace(x) && !string.IsNullOrEmpty(x)).ToList();

            //need to add parts in parantheses back together

            string valuesInParantheses = "";

            //rewrite without shitty temp list
            var queryPartsWithP = new List<string>();

            bool startParantheses = false;

            foreach (var part in queryParts)
            {
                if(part.Contains(")"))
                {
                    startParantheses = false;
                    valuesInParantheses += part;
                    queryPartsWithP.Add(valuesInParantheses);
                    continue;
                }

                if(startParantheses)
                {
                    valuesInParantheses += part;
                }

                if(part.Contains("("))
                {
                    startParantheses = true;
                    valuesInParantheses += part;
                }
                else
                {
                    queryPartsWithP.Add(part);
                }

            }


            var whereClauseIndex = IndexOfWhereClause(query, GetTableName(query));

            if(whereClauseIndex == -1)
            {
                return new List<string>();
            }

            string firstPredicate = queryPartsWithP[whereClauseIndex + 0] + " " + 
                                    queryPartsWithP[whereClauseIndex + 1] + " " +
                                    queryPartsWithP[whereClauseIndex + 2] + " " +
                                    queryPartsWithP[whereClauseIndex + 3];

            predicates.Add(firstPredicate);

            int operatorIndex = whereClauseIndex + 4;

            while (operatorIndex < queryPartsWithP.Count())
            {
                string currentPredicate = queryPartsWithP[operatorIndex + 0] + " " +
                                          queryPartsWithP[operatorIndex + 1] + " " +
                                          queryPartsWithP[operatorIndex + 2] + " " +
                                          queryPartsWithP[operatorIndex + 3];

                predicates.Add(currentPredicate);

                operatorIndex += 4;
            }

            return predicates;
        }

        public InnerStatement GetInnerMostSelectStatement(string query)
        {
            int? indexOfLastOpeningParantheses = null;
            int? indexOfClosingParantheses = null;

            int endParanthesesToSkip = 0;

            for (int i = 0; i < query.Length; i++)
            {
                if (query[i] == '(')
                {
                    //succeeded by select?
                    if(SucceededBySelect(query, i))
                    {
                        indexOfLastOpeningParantheses = i;
                    }
                    else
                    {
                        endParanthesesToSkip += 1;
                    }
                   
                }

                if (query[i] == ')')
                {
                    if(endParanthesesToSkip == 0)
                    {
                        indexOfClosingParantheses = i;
                        break;
                    }

                    endParanthesesToSkip -= 1;
                }

            }

            if (!indexOfLastOpeningParantheses.HasValue)
            {
                return null;
            }


            string subQuery = query.Substring((int)indexOfLastOpeningParantheses + 1, (int)(indexOfClosingParantheses - indexOfLastOpeningParantheses - 1));

            return new InnerStatement
            {
                Statement = subQuery,
                StartIndexOfOpenParantheses = (int)indexOfLastOpeningParantheses,
                EndIndexOfCloseParantheses = (int)indexOfClosingParantheses
            };
        }

        public bool SucceededBySelect(string str, int idx)
        {
            var sub = str.Substring(idx);

            sub = sub.Replace(" ", "").Replace("\r\n", "");

            return sub.Length > 7 && sub.Substring(0, 7) == "(select";
        }

    }
}
