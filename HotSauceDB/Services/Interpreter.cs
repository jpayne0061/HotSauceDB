﻿using HotSauceDB.Models;
using HotSauceDB.Models.Transactions;
using HotSauceDB.Services.Parsers;
using HotSauceDb.Enums;
using HotSauceDb.Models;
using HotSauceDb.Models.Transactions;
using HotSauceDb.Services.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HotSauceDb.Services
{
    public class Interpreter
    {
        private SelectParser _selectParser;
        private InsertParser _insertParser;
        private SchemaFetcher _schemaFetcher;
        private GeneralParser _generalParser;
        private CreateParser _createParser;
        private Reader _reader;
        private LockManager _lockManager;
        private IndexPage _indexPage;

        public Interpreter(SelectParser selectParser, 
                            InsertParser insertParser, 
                            SchemaFetcher schemaFetcher,
                            GeneralParser generalParser,
                            CreateParser createParser,
                            LockManager lockManager,
                            Reader reader)
        {
            _selectParser = selectParser;
            _insertParser = insertParser;
            _schemaFetcher = schemaFetcher;
            _generalParser = generalParser;
            _createParser = createParser;
            _lockManager = lockManager;
            _reader = reader;
        }

        public TableDefinition GetTableDefinition(string tableName)
        {
            return _schemaFetcher.GetTableDefinition(tableName);
        }

        public object ProcessStatement(string sql)
        {
            string sqlStatementType = _generalParser.GetSqlStatementType(sql);

            switch(sqlStatementType)
            {
                case "select":
                    return RunQueryAndSubqueries(sql);
                case "insert":
                    return RunInsertStatement(sql);
                case "create":
                    return RunCreateTableStatement(sql);
                case "update":
                    return RunUpdateStatement(sql);
                case "alter":
                    return RunAlterTable(sql);
            }

            throw new Exception("Invalid query. Query must start with 'select' or 'insert'");
        }

        private object RunAlterTable(string sql)
        {
            var parser = new AlterParser();

            var tableName = parser.GetTableName(sql);

            var tableDef = _schemaFetcher.GetTableDefinition(tableName);

            byte newColumnIndex = tableDef.ColumnDefinitions.Select(c => c.Index).Max();

            newColumnIndex++;

            ColumnDefinition columnDefinition = parser.GetNewColumnDefinition(sql, newColumnIndex);

            if(tableDef.ColumnDefinitions.Select(c => c.ColumnName.ToLower()).Contains(columnDefinition.ColumnName.ToLower()))
            {
                throw new Exception($"table {tableDef.TableName} already contains a column named {columnDefinition.ColumnName}");
            }

            return RunAlterTable(tableDef, columnDefinition);
        }

        private object RunUpdateStatement(string sql)
        {
            var parser = new UpdateParser();

            List<KeyValuePair<string, string>> keyValuePairs = parser.GetUpdates(sql);

            var tableName = parser.GetTableName(sql);

            var tableDef = _schemaFetcher.GetTableDefinition(tableName);

            PredicateStep predicateStep = parser.ParsePredicates(sql);

            var predicateOperations = BuildDelagatesFromPredicates(tableName, predicateStep.Predicates);

            var selects = tableDef.ColumnDefinitions
                            .Select(x => new SelectColumnDto(x)).OrderBy(x => x.Index).ToList();

            selects.ForEach(x => x.IsInSelect = true);

            var readTransaction = new ReadTransaction
            {
                TableDefinition = tableDef,
                Selects = selects,
                PredicateOperations = predicateOperations
            };

            var selectData = RunReadTransaction(readTransaction);

            var columnDefs = new List<ColumnDefinition>();

            foreach (var col in keyValuePairs)
            {
                var colDef = tableDef.ColumnDefinitions.Where(x => x.ColumnName.ToLower() == col.Key).Single();

                columnDefs.Add(colDef);
            }

            Converter converter = new Converter();

            Dictionary<int, IComparable> indexToValue = new Dictionary<int, IComparable>();

            foreach (var columnNameToValue in keyValuePairs)
            {
                var colDef = tableDef.ColumnDefinitions.Where(x => x.ColumnName.ToLower() == columnNameToValue.Key).Single();

                indexToValue[colDef.Index] = converter.ConvertToType(columnNameToValue.Value, colDef.Type);
            }

            foreach (var colDef in columnDefs)
            {
                foreach (var row in selectData.Rows)
                {
                    row[colDef.Index] = indexToValue[colDef.Index];
                }
            }

            //each row is updated as its own transaction - not ideal for update - not atomic
            for (int i = 0; i < selectData.Rows.Count; i++)
            {
                var writeTransaction = new WriteTransaction
                {
                    Data = selectData.Rows[i].ToArray(),
                    TableDefinition = tableDef,
                    AddressToWriteTo = selectData.RowLocations[i],
                    Query = sql,
                    UpdateObjectCount = false
                };

                _lockManager.ProcessWriteTransaction(writeTransaction);
            }

            return new object();
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

            //storing an inenumerable in a temporary list and then adding the "then by"
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

        //break grouping / order by logic into their own classes
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

            if(groupParts.Count() > 2)
            {
                throw new Exception("Only one group by column allowed");
            }

            string groupingColumn = groupParts[1].ToLower();

            selects = selects.Where(x => x.IsInSelect);

            int groupColumnIndex = selects.Select(x => x.ColumnName.ToLower()).ToList().IndexOf(groupingColumn);

            var columnIndexToAggregateFunction = selects.Select((x, i) => new KeyValuePair<int, Func<List<IComparable>, IComparable>>(i, x.AggregateFunction)).ToList();

            var groupings = rows.GroupBy(x => x[groupColumnIndex]);

            var aggregatedRows = new List<List<IComparable>>();


            foreach (var grouping in groupings)
            {
                List<List<IComparable>> groupingValues = grouping.Select(x => x).ToList();

                List<IComparable> groupedRow = ReturnGroupedList(grouping.Key, groupingValues, columnIndexToAggregateFunction);

                aggregatedRows.Add(groupedRow);
            }

            return aggregatedRows;
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
                        select.AggregateFunction = firstMatchingColumn.AggregateFunction;
                    }
                }
            }

            PredicateStep predicateStep = _selectParser.ParsePredicates(query);

            predicateStep = _selectParser.GetPredicateTrailers(predicateStep, query);

            var predicateOperations = BuildDelagatesFromPredicates(tableName, predicateStep.Predicates);

            //queue transaction
            //subscribe to event being read/finished
            var readTransaction = new ReadTransaction
            {
                TableDefinition = tableDef,
                Selects = selects,
                PredicateOperations = predicateOperations
            };

            var rows = RunReadTransaction(readTransaction).Rows;

            if (predicateStep.PredicateTrailer != null && predicateStep.PredicateTrailer.Any())
            {
                rows = ProcessPostPredicateGroupBy(selects, predicateStep, rows);
                rows = ProcessPostPredicateOrderBy(selects, predicateStep, rows);
            }

            return rows;
        }

        public SelectData RunReadTransaction(ReadTransaction readTransaction)
        {
            return _lockManager.ProcessReadTransaction(readTransaction);
        }

        public List<List<IComparable>> RunQueryAndSubqueries(string query)
        {
            var subQuery = _selectParser.GetInnerMostSelectStatement(query);

            if(subQuery != null)
            {
                var tableName = _selectParser.GetTableName(subQuery.Statement);

                IList<string> subQueryColumns = _selectParser.GetColumns(subQuery.Statement).Select(x => x.ColumnName).ToList();

                if(_indexPage == null)
                {
                    _indexPage = _reader.GetIndexPage();
                }

                var tableDef = _indexPage.TableDefinitions.Where(x => x.TableName == tableName).FirstOrDefault();

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

        public string ReplaceSubqueryWithValue(string query, InnerStatement subquery, string value, TypeEnum type)
        {
            string subQueryWithParantheses = query.Substring(subquery.StartIndexOfOpenParantheses,
                subquery.EndIndexOfCloseParantheses - subquery.StartIndexOfOpenParantheses + 1);

            if(type == TypeEnum.String)
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

            if(_indexPage == null)
            {
                _indexPage = _reader.GetIndexPage();
            }

            var tableDefinition = _indexPage.TableDefinitions.Where(x => x.TableName == tableName).FirstOrDefault();

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

            return RunInsert(row, tableName, dml);
        }

        public InsertResult RunInsert(IComparable[] row, string tableName, string dml = null)
        {
            try
            {
                TableDefinition tableDef = _schemaFetcher.GetTableDefinition(tableName);

                long? firstAvailableAddress = null;
                lock (_reader)
                {
                    firstAvailableAddress = _reader.GetFirstAvailableDataAddress(tableDef.DataAddress, tableDef.GetRowSizeInBytes());

                    var writeTransaction = new WriteTransaction
                    {
                        Data = row,
                        TableDefinition = tableDef,
                        AddressToWriteTo = (long)firstAvailableAddress,
                        Query = dml
                    };

                    return _lockManager.ProcessWriteTransaction(writeTransaction);
                }

            }
            catch (Exception ex)
            {
                return new InsertResult { Successful = false, ErrorMessage = ex.Message };
            }
        }

        public ResultMessage RunCreateTableStatement(string dml)
        {
            TableDefinition tableDef = new TableDefinition();

            tableDef.TableName = _createParser.GetTableName(dml);
            tableDef.ColumnDefinitions = _createParser.GetColumnDefintions(dml);

            return RunCreateTable(tableDef);
        }

        public ResultMessage RunCreateTable(TableDefinition tableDef)
        {
            SchemaTransaction dmlTransaction = new SchemaTransaction
            {
                TableDefinition = tableDef
            };

            ResultMessage msg = _lockManager.ProcessCreateTableTransaction(dmlTransaction);

            _schemaFetcher.RefreshIndexPage();
            _indexPage = _reader.GetIndexPage();

            return msg;
        }

        public IComparable ConvertToType(ColumnDefinition columnDefinition, string val)
        {
            IComparable convertedVal;

            switch (columnDefinition.Type)
            {
                case TypeEnum.Boolean:
                    convertedVal = Convert.ToBoolean(val);
                    break;
                case TypeEnum.Char:
                    convertedVal = Convert.ToChar(val);
                    break;
                case TypeEnum.Decimal:
                    convertedVal = Convert.ToDecimal(val);
                    break;
                case TypeEnum.Int32:
                    convertedVal = Convert.ToInt32(val);
                    break;
                case TypeEnum.Int64:
                    convertedVal = Convert.ToInt64(val);
                    break;
                case TypeEnum.String:
                    convertedVal = val.TrimStart('\'').TrimEnd('\'').PadRight(columnDefinition.ByteSize - 1, ' ');
                    break;
                case TypeEnum.DateTime:
                    convertedVal = Convert.ToDateTime(val);
                    break;
                default:
                    convertedVal = null;
                    break;
            }

            return convertedVal;
        }

        private ResultMessage RunAlterTable(TableDefinition tableDef, ColumnDefinition columnDefinition)
        {
            //check that table doesn't already contain column name
            //get data type from query (look at create table)
            AlterTableTransaction dmlTransaction = new AlterTableTransaction
            {
                TableDefinition = tableDef,
                NewColumn = columnDefinition
            };

            return _lockManager.ProcessAlterTableTransaction(dmlTransaction);
        }
    }
}
