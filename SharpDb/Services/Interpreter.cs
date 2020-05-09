using SharpDb.Enums;
using SharpDb.Models;
using SharpDb.Services.Parsers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpDb.Services
{
    public class Interpreter
    {
        private ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();

        private SelectParser _selectParser;
        private InsertParser _insertParser;
        private Reader _reader;
        private Writer _writer;
        private SchemaFetcher _schemaFetcher;
        private GeneralParser _generalParser;
        private CreateParser _createParser;

        public Interpreter(SelectParser selectParser, 
                            InsertParser insertParser, 
                            Reader reader, 
                            Writer writer, 
                            SchemaFetcher schemaFetcher,
                            GeneralParser generalParser,
                            CreateParser createParser)
        {
            _selectParser = selectParser;
            _insertParser = insertParser;
            _reader = reader;
            _writer = writer;
            _schemaFetcher = schemaFetcher;
            _generalParser = generalParser;
            _createParser = createParser;
        }

        public object ProcessStatement(string sql)
        {
            //_queue.Enqueue(sql);

            //while()
            //{

            //}

            //if(!_queue.IsEmpty)
            //{

            //}
            

            string sqlStatementType = _generalParser.GetSqlStatementType(sql);

            if(sqlStatementType == "select")
            {
                return RunQueryAndSubqueries(sql);
            }
            else if(sqlStatementType == "insert")
            {
                return RunInsertStatement(sql);
            }
            else if (sqlStatementType == "create")
            {
                return RunCreateTableStatement(sql);
            }

            throw new Exception("Invalid query. Query must start with 'select' or 'insert'");
        }

        private List<List<IComparable>> ProcessPostPredicateOrderBy(IEnumerable<SelectColumnDto> selects, PredicateStep predicateStep, List<List<IComparable>> rows)
        {
            //first, group by
            var orderByClause = predicateStep.PredicateTrailer.Where(x => x.Contains("order")).FirstOrDefault();

            if(orderByClause == null)
            {
                return rows;
            }

            var orderParts = orderByClause.Split(' ');

            //storing an inenumerable in a temporary list and trhen adding the "then by"
            //clause does not work. changes to an ienumerable are not guaranteed to persist

            switch (orderParts.Count())
            {
                case 2:
                    var select = selects.Where(x => x.ColumnName == orderParts[1]).FirstOrDefault();
                    rows = rows.OrderBy(x => x[select.Index]).ToList();
                    break;
                case 3:
                    var selectCase2 = selects.Where(x => x.ColumnName == orderParts[1]).FirstOrDefault();
                    var selectCase2_2 = selects.Where(x => x.ColumnName == orderParts[2]).FirstOrDefault();
                    rows = rows.OrderBy(x => x[selectCase2.Index]).ThenBy(x => x[selectCase2_2.Index]).ToList();
                    break;
                case 4:
                    var selectCase3 = selects.Where(x => x.ColumnName == orderParts[1]).FirstOrDefault();
                    var selectCase3_2 = selects.Where(x => x.ColumnName == orderParts[2]).FirstOrDefault();
                    var selectCase3_3 = selects.Where(x => x.ColumnName == orderParts[3]).FirstOrDefault();
                    rows = rows.OrderBy(x => x[selectCase3.Index]).ThenBy(x => x[selectCase3_2.Index]).
                        ThenBy(x => x[selectCase3_3.Index]).ToList();
                    break;
                default:
                    throw new Exception("There is a maximum of three columns allowed in the order by clause.");

            }

            return rows;
        }

        private List<IComparable> ReturnGroupedList(IComparable key, List<List<IComparable>> groupingValues,
                                                    IEnumerable<KeyValuePair<int, Func<List<IComparable>, IComparable>>> columnIndexesToAggregate)
        {

            var aggregatedRow = new List<IComparable> { key };

            foreach (var kvp in columnIndexesToAggregate)
            {
                if (kvp.Value == null) //column is not aggregated
                {
                    continue;
                }

                //select value from each list
                var valuesToGroupFromEachList = groupingValues.Select(x => x[kvp.Key]).ToList();

                var aggregateValue = kvp.Value(valuesToGroupFromEachList);

                aggregatedRow.Add(aggregateValue);
            }

            return aggregatedRow;
        }


        //Need to implement parsing of aggregate functions in order to do group by
        private List<List<IComparable>> ProcessPostPredicateGroupBy(IEnumerable<SelectColumnDto> selects, PredicateStep predicateStep, List<List<IComparable>> rows)
        {
            Func<IComparable, 
                List<List<IComparable>>, 
                IEnumerable<KeyValuePair<int, 
                Func<List<IComparable>, IComparable>>>, 
                List<IComparable>> processGrouping = ReturnGroupedList;


            //first, group by
            var groupByClause = predicateStep.PredicateTrailer.Where(x => x.Contains("group")).FirstOrDefault();

            if (groupByClause == null)
            {
                return rows;
            }

            var groupParts = groupByClause.Split(' ');

            //storing an inenumerable in a temporary list and trhen adding the "then by"
            //clause does not work. changes to an ienumerable are not guaranteed to persist


            switch (groupParts.Count())
            {
                case 2:

                    //need to pull out indexes (in the select list) of aggregated column

                    //need to pull out corresponding aggregate functions that align with each aggregated column

                    //ex: "select price, MAX(addreess), MIN(Bedrooms)"

                    //address - 1 - MAX
                    //bedrooms - 2 - MIN

                    string groupingColumn = groupParts[1].ToLower();

                    selects = selects.Where(x => x.IsInSelect);

                    int groupColumnIndex = selects.Select(x => x.ColumnName.ToLower()).ToList().IndexOf(groupingColumn);

                    var columnIndexToAggregateFunction = selects.Select((x, i) => new KeyValuePair<int, Func<List<IComparable>, IComparable>>(i, x.AggregateFunction)).ToList();

                    var groupings = rows.GroupBy(x => x[groupColumnIndex]);

                    var aggregatedRows = new List<List<IComparable>>();


                    foreach(var grouping in groupings)
                    {
                        List<List<IComparable>> groupingValues = grouping.Select(x => x).ToList();

                        List<IComparable> groupedRow = ReturnGroupedList(grouping.Key, groupingValues, columnIndexToAggregateFunction);

                        aggregatedRows.Add(groupedRow);
                    }

                    return aggregatedRows;
                case 9:
                    var selectCase2 = selects.Where(x => x.ColumnName == groupParts[1]).FirstOrDefault();
                    var selectCase2_2 = selects.Where(x => x.ColumnName == groupParts[2]).FirstOrDefault();
                    rows = rows.OrderBy(x => x[selectCase2.Index]).ThenBy(x => x[selectCase2_2.Index]).ToList();
                    break;
                case 4:
                    var selectCase3 = selects.Where(x => x.ColumnName == groupParts[1]).FirstOrDefault();
                    var selectCase3_2 = selects.Where(x => x.ColumnName == groupParts[2]).FirstOrDefault();
                    var selectCase3_3 = selects.Where(x => x.ColumnName == groupParts[2]).FirstOrDefault();
                    rows = rows.OrderBy(x => x[selectCase3.Index]).ThenBy(x => x[selectCase3_2.Index]).
                        ThenBy(x => x[selectCase3_3.Index]).ToList();
                    break;
                default:
                    throw new Exception("There is a maximum of three columns allowed in the order by clause.");

            }

            return rows;
        }

        public List<List<IComparable>> RunQuery(string query)
        {
            var tableName = _selectParser.GetTableName(query);

            var tableDef = _schemaFetcher.GetTableDefinition(tableName);

            List<SelectColumnDto> columns = _selectParser.GetColumns(query);

            IEnumerable<SelectColumnDto> selects = tableDef.ColumnDefinitions
                .Select(x => new SelectColumnDto(x)).OrderBy(x => x.Index).ToList();

            foreach(var select in selects)
            {
                if(columns.Select(x => x.ColumnName).Contains(select.ColumnName) || columns.First().ColumnName == "*")
                {
                    select.IsInSelect = true;

                    SelectColumnDto firstMatchingColumn = columns.Where(x => x.ColumnName == select.ColumnName).FirstOrDefault();

                    if (firstMatchingColumn != null)
                    {
                        select.AggregateFunction = columns.Where(x => x.ColumnName == select.ColumnName).First().AggregateFunction;

                    }
                }
            }

            PredicateStep predicateStep = _selectParser.ParsePredicates(query);

            var predicateOperations = BuildDelagatesFromPredicates(tableName, predicateStep.Predicates);

            List<List<IComparable>> rows =  _reader.GetRows(tableDef, selects, predicateOperations);

            if(predicateStep.PredicateTrailer != null && predicateStep.PredicateTrailer.Any())
            {
                rows = ProcessPostPredicateOrderBy(selects, predicateStep, rows);
                rows = ProcessPostPredicateGroupBy(selects, predicateStep, rows);
            }

            return rows;
        }

        public List<List<IComparable>> RunQueryAndSubqueries(string query)
        {
            var reader = new Reader();

            var indexPage = reader.GetIndexPage();

            var subQuery = _selectParser.GetInnerMostSelectStatement(query);

            if(subQuery != null)
            {
                var tableName = _selectParser.GetTableName(subQuery.Statement);

                IList<string> subQueryColumns = _selectParser.GetColumns(subQuery.Statement).Select(x => x.ColumnName).ToList();

                var tableDef = indexPage.TableDefinitions.Where(x => x.TableName == tableName).FirstOrDefault();

                //only support for scalar subqueries, currently
                var subQueryColumn = tableDef.ColumnDefinitions
                    .Where(x => x.ColumnName == subQueryColumns[0].ToLower() || subQueryColumns[0] == "*").First();

                var subQueryValue = string.Join(",", RunQuery(subQuery.Statement).Select(x => x[0]));

                query = ReplaceSubqueryWithValue(query, subQuery, subQueryValue, subQueryColumn.Type);

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
            if(predicates == null || !predicates.Any())
            {
                return new List<PredicateOperation>();
            }

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


                predicateParts = _generalParser.CombineValuesInParantheses(predicateParts);

                var colDef = tableDefinition.ColumnDefinitions
                    .Where(x => x.ColumnName == predicateParts[1].ToLower()).FirstOrDefault();

                var operatorToDelegate = new Dictionary<string, Func<IComparable, object, bool>>
                {
                    { ">",   CompareDelegates.IsMoreThan },
                    { "<",   CompareDelegates.IsLessThan},
                    { "=",   CompareDelegates.IsEqualTo},
                    { ">=",  CompareDelegates.MoreThanOrEqualTo},
                    { "<=",  CompareDelegates.LessThanOrEqualTo},
                    { "!=",  CompareDelegates.NotEqualTo},
                    { "in",  CompareDelegates.Contains},
                };

                if(predicateParts[2].ToLower() == "in")
                {
                    string innerValue = predicateParts[3].Trim('(').Trim(')');

                    var list = new List<string>(innerValue.Split(','));

                    var targetList = list
                              .Select(x => ConvertToType(colDef, x))
                              .ToHashSet();

                    predicateOperations.Add(new PredicateOperation
                    {
                        Delegate = operatorToDelegate[predicateParts[2].ToLower()],
                        Predicate = predicates[i],
                        ColumnName = predicateParts[1],
                        Value = targetList,
                        Operator = predicateParts[0],
                        ColumnIndex = colDef.Index
                    });
                }
                else
                {
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

        public ResultMessage RunCreateTableStatement(string dml)
        {
            TableDefinition tableDef = new TableDefinition();

            tableDef.TableName = _createParser.GetTableName(dml);
            tableDef.ColumnDefinitions = _createParser.GetColumnDefintions(dml);

            return _writer.WriteTableDefinition(tableDef);
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
                case TypeEnums.DateTime:
                    convertedVal = Convert.ToDateTime(val);
                    break;
                default:
                    convertedVal = null;
                    break;
            }

            return convertedVal;
        }

    }
}
