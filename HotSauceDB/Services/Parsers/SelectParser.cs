using HotSauceDB.Statics;
using SharpDb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SharpDb.Services.Parsers
{
    public class SelectParser : GeneralParser
    {
        public List<SelectColumnDto> GetColumns(string query)
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

            return ParseAggregates(columnsSplit);
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

            return tableName.ToLower().Replace("\r\n", "");
        }

        public List<SelectColumnDto> ParseAggregates(List<string> columns)
        {
            List<SelectColumnDto> selectDtos = new List<SelectColumnDto>();

            columns = columns.Select(x => x.ToLower().Replace(" ", "").Replace("\r\n", "")).ToList();

            foreach (var col in columns)
            {
                InnerStatement innerStatement = GetFirstMostInnerParantheses(col);

                string columnName = innerStatement == null ? col : innerStatement.Statement;

                if(columnName == null)
                {
                    selectDtos.Add(new SelectColumnDto {
                        ColumnName = col
                    });
                    continue;
                }

                string aggregrateFunction = col.Replace(columnName, "");

                if(aggregrateFunction == "max()")
                {
                    selectDtos.Add(new SelectColumnDto
                    {
                        ColumnName = columnName,
                        AggregateFunction = CompareDelegates.Max
                    });
                }
                else if(aggregrateFunction == "min()")
                {
                    selectDtos.Add(new SelectColumnDto
                    {
                        ColumnName = columnName,
                        AggregateFunction = CompareDelegates.Min
                    });
                }
                else if (aggregrateFunction == "count()")
                {
                    selectDtos.Add(new SelectColumnDto
                    {
                        ColumnName = columnName,
                        AggregateFunction = CompareDelegates.Count
                    });
                }
                else
                {
                    selectDtos.Add(new SelectColumnDto
                    {
                        ColumnName = columnName,
                        AggregateFunction = null
                    });
                }

            }

            return selectDtos;
        }

        public int IndexOfWhereClause(string query, string tableName)
        {
            query = query.ToLower();

            List<string> queryParts = query.SplitOnWhiteSpace();

            int tableNameIndex = queryParts.IndexOf(tableName);

            if(queryParts.Count() > tableNameIndex + 1
                && queryParts[tableNameIndex + 1].ToLower() == "where")
            {
                return tableNameIndex + 1;
            }

            return -1;
        }

        public int IndexOfTableName(string query, string tableName)
        {
            query = query.ToLower();

            List<string> queryParts = query.Split(new[] { ' ', '\r', '\n'})
                .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

            int tableNameIndex = queryParts.IndexOf(tableName);

            return tableNameIndex;
        }

        public PredicateStep ParsePredicates(string query)
        {
            var predicates = new List<string>();


            List<string> queryParts = SplitOnWhiteSpaceExceptQuotesAndParantheses(query);

            var whereClauseIndex = IndexOfWhereClause(query, GetTableName(query));

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

            int startTrailingPredicate = operatorIndex == null ? IndexOfTableName(query, GetTableName(query)) + 1 : (int)operatorIndex;

            PredicateStep predicateStep = new PredicateStep();

            predicateStep.Predicates = predicates;
            predicateStep.HasPredicates = true;

            List<string> predicateTrailersUnparsed = string.Join(' ', queryParts.GetRange(startTrailingPredicate, queryParts.Count() - startTrailingPredicate)).ToLower()
                .Split(new[] { " ", ",", "by" }, StringSplitOptions.RemoveEmptyEntries).Where(x => !string.IsNullOrEmpty(x)).ToList();

            predicateStep.PredicateTrailer = ParsePredicateTrailers(predicateTrailersUnparsed);

            return predicateStep;
        }

        public List<string> SplitOnWhiteSpaceExceptQuotesAndParantheses(string query)
        {
            //https://stackoverflow.com/questions/14655023/split-a-string-that-has-white-spaces-unless-they-are-enclosed-within-quotes
            //https://stackoverflow.com/users/1284526/c%c3%a9dric-bignon
            var queryParts = query.Split("'")
             .Select((element, index) => index % 2 == 0  // If even index
                                   ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)  // Split the item
                                   : new string[] { "'" + element + "'" })  // Keep the entire item
             .SelectMany(element => element).Select(x => x.Replace("\r\n", "")).
             Where(x => !string.IsNullOrWhiteSpace(x) && !string.IsNullOrEmpty(x)).ToList();

            var queryPartsWithParantheses = CombineValuesInParantheses(queryParts);

            return queryPartsWithParantheses;
        }


        public List<string> ParsePredicateTrailers(List<string> predicateTrailers)
        {
            if(!predicateTrailers.Any())
            {
                return null;
            }

            predicateTrailers = predicateTrailers.Select(x => x.ToLower()).ToList();

            if(!Globals.PredicateTrailers.Contains(predicateTrailers[0]))
            {
                throw new Exception($"Invalid expression: {string.Join(' ', predicateTrailers)}");
            }

            List<string> parsedPredicates = new List<string>();

            string temp = "";

            for (int i = 0; i < predicateTrailers.Count(); i++)
            {
                if(Globals.PredicateTrailers.Contains(predicateTrailers[i]))
                {
                    if(temp != "")
                    {
                        parsedPredicates.Add(temp);
                    }
                    
                    temp = "";
                    temp = predicateTrailers[i];
                    continue;
                }

                temp += " " + predicateTrailers[i];
            }

            parsedPredicates.Add(temp);

            return parsedPredicates;
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
