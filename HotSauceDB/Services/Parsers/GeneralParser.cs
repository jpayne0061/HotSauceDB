using SharpDb.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpDb.Services.Parsers
{
    public class GeneralParser
    {
        public string ToLowerAndTrim(string query)
        {
            query = query.Trim();

            query = query.ToLower();

            return query;
        }

        public string GetSqlStatementType(string query)
        {
            query = ToLowerAndTrim(query);

            int numberCharsToTake = 6; //all statements contain 6 characters - 'select', 'update', 'create'...

            return TruncateLongString(query, numberCharsToTake).Trim();
        }

        public string TruncateLongString(string str, int maxLength)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            return str.Substring(0, Math.Min(str.Length, maxLength));
        }

        public InnerStatement GetFirstMostInnerParantheses(string query)
        {
            int? indexOfLastOpeningParantheses = null;
            int? indexOfClosingParantheses = null;

            for (int i = 0; i < query.Length; i++)
            {
                if (query[i] == '(')
                {
                    indexOfLastOpeningParantheses = i;
                }

                if (query[i] == ')' && indexOfLastOpeningParantheses != null)
                {
                    indexOfClosingParantheses = i;
                    break;
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

        public List<string> CombineValuesInParantheses(List<string> parts)
        {
            string valuesInParantheses = "";

            //rewrite without shitty temp list
            var queryPartsWithParantheses = new List<string>();

            bool startParantheses = false;

            foreach (var part in parts)
            {
                if (part.Contains(")"))
                {
                    startParantheses = false;
                    valuesInParantheses += part;
                    queryPartsWithParantheses.Add(valuesInParantheses);
                    continue;
                }

                if (startParantheses)
                {
                    valuesInParantheses += part;
                    continue;
                }

                if (part.Contains("("))
                {
                    startParantheses = true;
                    valuesInParantheses += part;
                }
                else
                {
                    queryPartsWithParantheses.Add(part);
                }

            }

            return queryPartsWithParantheses;
        }

        public InnerStatement GetOuterMostParantheses(string query)
        {
            int? indexFirstParantheses = null;
            int? indexOfClosingParantheses = null;

            for (int i = 0; i < query.Length; i++)
            {
                if (query[i] == '(')
                {
                    indexFirstParantheses = i;
                    break;
                }
            }

            for (int i = query.Length - 1; i >= 0; i--)
            {
                if (query[i] == ')')
                {
                    indexOfClosingParantheses = i;
                    break;
                }
            }

            if (!indexFirstParantheses.HasValue)
            {
                return null;
            }


            string subQuery = query.Substring((int)indexFirstParantheses + 1, (int)(indexOfClosingParantheses - indexFirstParantheses - 1));

            return new InnerStatement
            {
                Statement = subQuery,
                StartIndexOfOpenParantheses = (int)indexFirstParantheses,
                EndIndexOfCloseParantheses = (int)indexOfClosingParantheses
            };
        }

        public List<string> SplitOnSeparatorsExceptQuotesAndParantheses(string query, char[] separators)
        {
            //https://stackoverflow.com/questions/14655023/split-a-string-that-has-white-spaces-unless-they-are-enclosed-within-quotes
            //https://stackoverflow.com/users/1284526/c%c3%a9dric-bignon
            var queryParts = query.Split("'")
             .Select((element, index) => index % 2 == 0  
                                   ? element.Split(separators, StringSplitOptions.RemoveEmptyEntries) 
                                   : new string[] { "'" + element + "'" })  
             .SelectMany(element => element).Select(x => x.Replace("\r\n", "")).
             Where(x => !string.IsNullOrWhiteSpace(x) && !string.IsNullOrEmpty(x)).ToList();

            var queryPartsWithParantheses = CombineValuesInParantheses(queryParts);

            return queryPartsWithParantheses;
        }

        public PredicateStep ParsePredicates(string query)
        {
            var predicates = new List<string>();


            List<string> queryParts = SplitOnSeparatorsExceptQuotesAndParantheses(query, new char[] { ' ', '\r', '\n'});

            var whereClauseIndex = queryParts.IndexOf("where");

            int? operatorIndex = null;

            if (whereClauseIndex != -1)
            {
                string firstPredicate = queryParts[whereClauseIndex + 0] + " " +
                                        queryParts[whereClauseIndex + 1] + " " +
                                        queryParts[whereClauseIndex + 2] + " " +
                                        queryParts[whereClauseIndex + 3];

                predicates.Add(firstPredicate);

                operatorIndex = whereClauseIndex + 4;

                HashSet<string> andOrOps = new HashSet<string> { "or", "and" };

                while (operatorIndex < queryParts.Count() && andOrOps.Contains(queryParts[(int)operatorIndex].ToLower()))
                {
                    string currentPredicate = queryParts[(int)operatorIndex + 0] + " " +
                                              queryParts[(int)operatorIndex + 1] + " " +
                                              queryParts[(int)operatorIndex + 2] + " " +
                                              queryParts[(int)operatorIndex + 3];

                    predicates.Add(currentPredicate);

                    operatorIndex += 4;
                }
            }

            PredicateStep predicateStep = new PredicateStep();

            predicateStep.Predicates = predicates;
            predicateStep.HasPredicates = true;
            predicateStep.OperatorIndex = operatorIndex;
            predicateStep.QueryParts = queryParts;

            return predicateStep;
        }
    }
}
