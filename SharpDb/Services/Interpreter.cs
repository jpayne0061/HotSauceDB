using SharpDb.Enums;
using SharpDb.Models;
using SharpDb.Services.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpDb.Services
{
    public class Interpreter
    {
        private SelectParser _selectParser;

        public Interpreter(SelectParser selectParser)
        {
            _selectParser = selectParser;
        }

        public List<List<IComparable>> RunQuery(string query)
        {
            var reader = new Reader();

            var tableName = _selectParser.GetTableName(query);

            var predicates = _selectParser.ParsePredicates(query);

            var predicateOperations = BuildDelagatesFromPredicates(tableName, predicates);

            return reader.GetRows(tableName, predicateOperations);
        }

        public List<PredicateOperation> BuildDelagatesFromPredicates(string tableName, List<string> predicates)
        {
            var reader = new Reader();

            var indexPage = reader.GetIndexPage();

            var tableDefinition = indexPage.TableDefinitions.Where(x => x.TableName == tableName).FirstOrDefault();

            var predicateOperations = new List<PredicateOperation>();

            for (int i = 0; i < predicates.Count(); i++)
            {
                List<string> predicateParts = predicates[i].Split("'")
             .Select((element, index) => index % 2 == 0  // If even index
                                   ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)  // Split the item
                                   : new string[] { "'" + element + "'" })  // Keep the entire item
             .SelectMany(element => element).Select(x => x.Replace("\r\n", "")).ToList();

                var colDef = tableDefinition.ColumnDefinitions
                    .Where(x => x.ColumnName.ToLower() == predicateParts[1].ToLower()).FirstOrDefault();

                switch (predicateParts[2])
                {
                    case ">":
                        predicateOperations.Add(new PredicateOperation
                        {
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
                case TypeEnums.Boolean:
                    convertedVal = Convert.ToBoolean(val);
                    break;
                case TypeEnums.Char:
                    convertedVal = Convert.ToChar(val);
                    break;
                case TypeEnums.Decimal:
                    convertedVal = Convert.ToDecimal(val);
                    break;
                case TypeEnums.Int32:
                    convertedVal = Convert.ToInt32(val);
                    break;
                case TypeEnums.Int64:
                    convertedVal = Convert.ToInt64(val);
                    break;
                case TypeEnums.String:
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
