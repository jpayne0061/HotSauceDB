﻿using HotSauceDb.Enums;
using HotSauceDb.Models;
using HotSauceDb.Statics;
using HotSauceDB.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HotSauceDb.Services.Parsers
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

        public PredicateStep GetPredicateTrailers(PredicateStep predicateStep, string query)
        {
            int startTrailingPredicate = predicateStep.OperatorIndex == null ? 
                IndexOfTableName(query, GetTableName(query)) + 1 : (int)predicateStep.OperatorIndex;

            List<string> predicateTrailersUnparsed = string.Join(' ', 
                                    predicateStep.QueryParts.GetRange(startTrailingPredicate, predicateStep.QueryParts.Count() - startTrailingPredicate))
                                    .ToLower()
                                    .Split(new[] { " ", ",", "by" }, StringSplitOptions.RemoveEmptyEntries)
                                    .Where(x => !string.IsNullOrEmpty(x)).ToList();

            predicateStep.PredicateTrailer = ParsePredicateTrailers(predicateTrailersUnparsed);

            return predicateStep;
        }


        public List<string> ParsePredicateTrailers(List<string> predicateTrailers)
        {
            if(!predicateTrailers.Any())
            {
                return null;
            }

            predicateTrailers = predicateTrailers.Select(x => x.ToLower()).ToList();

            if(!Constants.Predicate_Trailers.Contains(predicateTrailers[0]))
            {
                throw new Exception($"Invalid expression: {string.Join(' ', predicateTrailers)}");
            }

            List<string> parsedPredicates = new List<string>();

            string temp = "";

            for (int i = 0; i < predicateTrailers.Count(); i++)
            {
                if(Constants.Predicate_Trailers.Contains(predicateTrailers[i]))
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
                else if (query[i] == ')')
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

        internal string ReplaceSubqueryWithValue(string query, InnerStatement subquery, string value, TypeEnum type)
        {
            string subQueryWithParantheses = query.Substring(subquery.StartIndexOfOpenParantheses,
                subquery.EndIndexOfCloseParantheses - subquery.StartIndexOfOpenParantheses + 1);

            if (type == TypeEnum.String)
            {
                value = "'" + value + "'";
            }

            return query.Replace(subQueryWithParantheses, value);
        }

    }
}
