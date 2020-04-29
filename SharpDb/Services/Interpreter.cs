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
            //get first inner most select


            //get value

            //replace original query with value

            //if string, wrap in quotes

            var reader = new Reader();

            var tableName = _selectParser.GetTableName(query);

            var indexPage = reader.GetIndexPage();

            var tableDef = indexPage.TableDefinitions.Where(x => x.TableName == tableName).FirstOrDefault();

            HashSet<string> columns = _selectParser.GetColumns(query).Select(x => x.ToLower()).ToHashSet();

            IEnumerable<SelectColumnDto> selects = tableDef.ColumnDefinitions.Select(x => new SelectColumnDto(x)).OrderBy(x => x.Index).ToList();

            foreach(var select in selects)
            {
                if(columns.Contains(select.ColumnName) || columns.First() == "*")
                {
                    select.IsInSelect = true;
                }
            }

            var predicates = _selectParser.ParsePredicates(query);

            var predicateOperations = BuildDelagatesFromPredicates(tableName, predicates);

            return reader.GetRows(tableDef, selects, predicateOperations);
        }

        public List<List<IComparable>> RunQueryWithSubQueries(string query)
        {

            var reader = new Reader();

            var indexPage = reader.GetIndexPage();

            var subQuery = _selectParser.GetFirstMostInnerSelectStatement(query);

            var hasSubquery = subQuery != null;

            while(hasSubquery)
            {
                var tableName = _selectParser.GetTableName(subQuery.Query);

                IList<string> subQueryColumns = _selectParser.GetColumns(subQuery.Query);

                var tableDef = indexPage.TableDefinitions.Where(x => x.TableName == tableName).FirstOrDefault();

                //only support for scalar subqueries, currently
                var subQueryColumn = tableDef.ColumnDefinitions.Where(x => x.ColumnName == subQueryColumns[0].ToLower()).First();

                var subQueryScalar = RunQuery(subQuery.Query)[0][0];

                query = ReplaceSubqueryWithValue(query, subQuery, subQueryScalar.ToString(), subQueryColumn.Type);

                subQuery = _selectParser.GetFirstMostInnerSelectStatement(query);

                hasSubquery = subQuery != null;
            }

            var tableNameFinalSelect = _selectParser.GetTableName(query);

            var tableDefFinalSelect = indexPage.TableDefinitions.Where(x => x.TableName == tableNameFinalSelect).FirstOrDefault();

            HashSet<string> columns = _selectParser.GetColumns(query).Select(x => x.ToLower()).ToHashSet();

            IEnumerable<SelectColumnDto> selects = tableDefFinalSelect.ColumnDefinitions.Select(x => new SelectColumnDto(x)).OrderBy(x => x.Index).ToList();

            foreach (var select in selects)
            {
                if (columns.Contains(select.ColumnName) || columns.First() == "*")
                {
                    select.IsInSelect = true;
                }
            }

            var predicates = _selectParser.ParsePredicates(query);

            var predicateOperations = BuildDelagatesFromPredicates(tableNameFinalSelect, predicates);

            return reader.GetRows(tableDefFinalSelect, selects, predicateOperations);
        }

        public string ReplaceSubqueryWithValue(string query, Subquery subquery, string value, TypeEnums type)
        {
            string subQueryWithParantheses = query.Substring(subquery.StartIndexOfOpenParantheses,
                subquery.EndIndexOfCloseParantheses - subquery.StartIndexOfOpenParantheses + 1);

            if(type == TypeEnums.String)
            {
                value = "'" + value + "'";
            }

            return query.Replace(subQueryWithParantheses, value);
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
                    .Where(x => x.ColumnName == predicateParts[1].ToLower()).FirstOrDefault();

                var operatorToDelegate = new Dictionary<string, Func<IComparable, IComparable, bool>>
                {
                    { ">",   ConditionExecutor.IsMoreThan },
                    { "<",   ConditionExecutor.IsLessThan},
                    { "=",   ConditionExecutor.IsEqualTo},
                    { ">=",  ConditionExecutor.MoreThanOrEqualTo},
                    { "<=",  ConditionExecutor.LessThanOrEqualTo},
                    { "!=",  ConditionExecutor.NotEqualTo},
                };

                predicateOperations.Add(new PredicateOperation
                {
                    Delegate = operatorToDelegate[predicateParts[2]],
                    Predicate = predicates[i],
                    ColumnName = predicateParts[1],
                    Value = ConvertToType(colDef, predicateParts[3]),
                    Operator = predicateParts[0],
                    ColumnIndex = colDef.Index
                });
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
                    convertedVal = val.TrimStart('\'').TrimEnd('\'').PadRight(columnDefinition.ByteSize - 1, ' ');
                    break;
                default:
                    convertedVal = null;
                    break;
            }

            return convertedVal;
        }

    }
}
