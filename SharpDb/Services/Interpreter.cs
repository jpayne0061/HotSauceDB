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
        private InsertParser _insertParser;
        private Reader _reader;
        private Writer _writer;
        private SchemaFetcher _schemaFetcher;
        private GeneralParser _generalParser;

        public Interpreter(SelectParser selectParser, 
                            InsertParser insertParser, 
                            Reader reader, 
                            Writer writer, 
                            SchemaFetcher schemaFetcher,
                            GeneralParser generalParser)
        {
            _selectParser = selectParser;
            _insertParser = insertParser;
            _reader = reader;
            _writer = writer;
            _schemaFetcher = schemaFetcher;
            _generalParser = generalParser;
        }

        public object ProcessStatement(string sql)
        {
            string sqlStatementType = _generalParser.GetSqlStatementType(sql);

            if(sqlStatementType == "select")
            {
                return RunQueryAndSubqueries(sql);
            }
            else if(sqlStatementType == "insert")
            {
                return RunInsertStatement(sql);
            }

            throw new Exception("Invalid query. Query must start with 'select' or 'insert'");
        }

        public List<List<IComparable>> RunQuery(string query)
        {
            var tableName = _selectParser.GetTableName(query);

            var tableDef = _schemaFetcher.GetTableDefinition(tableName);

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

            return _reader.GetRows(tableDef, selects, predicateOperations);
        }

        public List<List<IComparable>> RunQueryAndSubqueries(string query)
        {
            var reader = new Reader();

            var indexPage = reader.GetIndexPage();

            var subQuery = _selectParser.GetFirstMostInnerParantheses(query);

            if(subQuery != null)
            {
                var tableName = _selectParser.GetTableName(subQuery.Query);

                IList<string> subQueryColumns = _selectParser.GetColumns(subQuery.Query);

                var tableDef = indexPage.TableDefinitions.Where(x => x.TableName == tableName).FirstOrDefault();

                //only support for scalar subqueries, currently
                var subQueryColumn = tableDef.ColumnDefinitions
                    .Where(x => x.ColumnName == subQueryColumns[0].ToLower() || subQueryColumns[0] == "*").First();

                var subQueryScalar = RunQuery(subQuery.Query)[0][0];

                query = ReplaceSubqueryWithValue(query, subQuery, subQueryScalar.ToString(), subQueryColumn.Type);

               return RunQueryAndSubqueries(query);
            }
            else
            {
                return RunQuery(query);
            }

        }

        public string ReplaceSubqueryWithValue(string query, InnerStatement subquery, string value, TypeEnums type)
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
                    { ">",   CompareDelegates.IsMoreThan },
                    { "<",   CompareDelegates.IsLessThan},
                    { "=",   CompareDelegates.IsEqualTo},
                    { ">=",  CompareDelegates.MoreThanOrEqualTo},
                    { "<=",  CompareDelegates.LessThanOrEqualTo},
                    { "!=",  CompareDelegates.NotEqualTo},
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

        public InsertResult RunInsertStatement(string dml)
        {
            IComparable[] row = _insertParser.GetRow(dml);

            string tableName = _insertParser.ParseTableName(dml);

            try
            {
                _writer.WriteRow(row, _schemaFetcher.GetTableDefinition(tableName));
            }
            catch(Exception ex)
            {

                return new InsertResult { Successful = false, ErrorMessage = ex.Message };
            }

            return new InsertResult { Successful = true };
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
