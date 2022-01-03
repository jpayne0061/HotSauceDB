using HotSauceDb.Enums;
using HotSauceDb.Models;
using HotSauceDb.Services;
using HotSauceDB.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HotSauceDB.Services
{
    public class DataMigrator
    {
        private readonly Interpreter _interpreter;

        public DataMigrator(Interpreter interpreter)
        {
            _interpreter = interpreter;
        }

        public void MigrateData<T>(string tableName, TableDefinition newTableDefinition) where T : class, new()
        {
            TableDefinition tableDefinition = _interpreter.GetTableDefinition(tableName);

            var oldRows = _interpreter.RunQuery($"select * from {tableName}");

            tableDefinition.TableName = new string(Guid.NewGuid().ToString().Take(tableName.Count()).ToArray());
            _interpreter.RenameTable(tableDefinition);

            var newTableData = ConvertTableDataToNewSchema(oldRows, newTableDefinition.ColumnDefinitions, tableDefinition.ColumnDefinitions);

            foreach (var row in newTableData)
            {
                _interpreter.RunInsert(row, tableName);
            }
        }

        public List<IComparable[]> ConvertTableDataToNewSchema(List<List<IComparable>> oldRows,
                                                               List<ColumnDefinition> newColumnDefinitions,
                                                               List<ColumnDefinition> oldColumnDefinitions)
        {
            var nameAndTypeToNewColumnDefinition = newColumnDefinitions.ToDictionary(x => Tuple.Create(x.ColumnName.ToLower(), x.Type), x => x);

            var indexToNewColumnDefinition = newColumnDefinitions.ToDictionary(x => x.Index, x => x);

            var indexToDefinitionOld = oldColumnDefinitions.ToDictionary(x => x.Index, x => x);

            List<IComparable[]> newRows = new List<IComparable[]>();

            for (int i = 0; i < oldRows.Count; i++)
            {
                IComparable[] newRow = new IComparable[newColumnDefinitions.Count];

                var populatedIndexes = new HashSet<byte>();

                //fill in values for columns that exist in both
                for (byte j = 0; j < oldRows[i].Count; j++)
                {
                    ColumnDefinition oldColDefinition = indexToDefinitionOld[j];

                    Tuple<string, TypeEnum> columnKey = Tuple.Create(oldColDefinition.ColumnName.ToLower(), oldColDefinition.Type);

                    if (!nameAndTypeToNewColumnDefinition.ContainsKey(columnKey))
                    {
                        continue;
                    }

                    ColumnDefinition newDefinition = nameAndTypeToNewColumnDefinition[columnKey];

                    byte newColumnIndex = newDefinition.Index;

                    newRow[newColumnIndex] = oldRows[i][j];

                    if (newDefinition.Type == TypeEnum.String && newDefinition.ByteSize != ((string)oldRows[i][j]).Length)
                    {
                        newRow[newColumnIndex] = GetResizedStringValue((string)oldRows[i][j], newDefinition);
                    }

                    populatedIndexes.Add(newColumnIndex);
                }

                //set default values for newly added columns
                for (byte j = 0; j < newRow.Length; j++)
                {
                    if (!populatedIndexes.Contains(j))
                    {
                        IComparable val = DefaultValues.GetDefaultDotNetValueForType(indexToNewColumnDefinition[j].Type);

                        newRow[j] = val;
                    }
                }

                newRows.Add(newRow);
            }

            return newRows;
        }

        private string GetResizedStringValue(string oldValue, ColumnDefinition newColumnDefinition)
        {
            string newString = string.Empty;

            if (newColumnDefinition.ByteSize > oldValue.Length)
            {
                newString = oldValue.PadRight(newColumnDefinition.ByteSize - 1);
            }
            else if (newColumnDefinition.ByteSize < oldValue.Length)
            {
                newString = oldValue.Substring(0, newColumnDefinition.ByteSize - 1);
            }

            return newString;
        }
    }
}
