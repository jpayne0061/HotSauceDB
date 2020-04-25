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
            int startIndex = query.IndexOf("from") + 4;

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
                && queryParts[tableNameIndex + 1] == "where")
            {
                return tableNameIndex + 1;
            }

            return -1;
        }

        public List<string> ParsePredicates(string query)
        {
            query = query.ToLower();

            var predicates = new List<string>();

            //need to rewrite ignore spaces in strings
            var queryParts = query.Split(' ')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Replace("\r\n", "")).ToList();

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

        public List<PredicateOperation> BuildDelagatesFromPredicates(string tableName, List<string> predicates)
        {
            var reader = new Reader();

            var indexPage = reader.GetIndexPage();

            var tableDefinition = indexPage.TableDefinitions.Where(x => x.TableName == tableName).FirstOrDefault();

            var predicateOperations = new List<PredicateOperation>();

            for(int i = 0; i < predicates.Count(); i++)
            {
                string[] predicateParts = predicates[i].Split(' ');

                var colDef = tableDefinition.ColumnDefinitions
                    .Where(x => x.ColumnName.ToLower() == predicateParts[1].ToLower()).FirstOrDefault();

                switch(predicateParts[2])
                {
                    case ">":
                        predicateOperations.Add(new PredicateOperation {
                            Delegate = ConditionExecutor.IsMoreThan,
                            Predicate = predicates[i],
                            ColumnName = predicateParts[1],
                            Value = ConvertToType(colDef, predicateParts[3]),
                            Operator = predicateParts[0],
                            ColumnIndex = colDef.Index
                        });
                        break;
                    case "<":
                        predicateOperations.Add(new PredicateOperation
                        {
                            Delegate = ConditionExecutor.IsLessThan,
                            Predicate = predicates[i],
                            ColumnName = predicateParts[1],
                            Value = ConvertToType(colDef, predicateParts[3]),
                            Operator = predicateParts[0],
                            ColumnIndex = colDef.Index
                        });
                        break;
                    case "=":
                        predicateOperations.Add(new PredicateOperation
                        {
                            Delegate = ConditionExecutor.IsEqualTo,
                            Predicate = predicates[i],
                            ColumnName = predicateParts[1],
                            Value = ConvertToType(colDef, predicateParts[3]),
                            Operator = predicateParts[0],
                            ColumnIndex = colDef.Index
                        });
                        break;
                    case ">=":
                        predicateOperations.Add(new PredicateOperation
                        {
                            Delegate = ConditionExecutor.MoreThanOrEqualTo,
                            Predicate = predicates[i],
                            ColumnName = predicateParts[1],
                            Value = ConvertToType(colDef, predicateParts[3]),
                            Operator = predicateParts[0],
                            ColumnIndex = colDef.Index
                        });
                        break;
                    case "<=":
                        predicateOperations.Add(new PredicateOperation
                        {
                            Delegate = ConditionExecutor.LessThanOrEqualTo,
                            Predicate = predicates[i],
                            ColumnName = predicateParts[1],
                            Value = ConvertToType(colDef, predicateParts[3]),
                            Operator = predicateParts[0],
                            ColumnIndex = colDef.Index
                        });
                        break;
                    case "!=":
                        predicateOperations.Add(new PredicateOperation
                        {
                            Delegate = ConditionExecutor.NotEqualTo,
                            Predicate = predicates[i],
                            ColumnName = predicateParts[1],
                            Value = ConvertToType(colDef, predicateParts[3]),
                            Operator = predicateParts[0],
                            ColumnIndex = colDef.Index
                        });
                        break;
                }
            }

            return predicateOperations;
        }

        public IComparable ConvertToType(ColumnDefinition columnDefinition, string val)
        {
            IComparable convertedVal;

            switch (columnDefinition.Type)
            {
                case 0:
                    convertedVal = Convert.ToBoolean(val);
                    break;
                case 1:
                    convertedVal = Convert.ToChar(val);
                    break;
                case 2:
                    convertedVal = Convert.ToDecimal(val);
                    break;
                case 3:
                    convertedVal = Convert.ToInt32(val);
                    break;
                case 4:
                    convertedVal = Convert.ToInt64(val);
                    break;
                case 5:
                    convertedVal = val.TrimStart('\'').TrimEnd('\'');
                    break;
                default:
                    convertedVal = null;
                    break;
            }

            return convertedVal;
        }

    }
}
