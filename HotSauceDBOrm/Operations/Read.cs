using HotSauceDb.Models;
using HotSauceDb.Services;
using HotSauceDb.Services.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HotSauceDbOrm.Operations
{
    public class Read : OperationsBase
    {   
        public Read(Interpreter interpreter) : base(interpreter)
        {
            _interpreter = interpreter;
        }

        public List<T> ReadRows<T>(string query) where T : new()
        {
            TableDefinition tableDef = _interpreter.GetTableDefinition(typeof(T).Name);

            List<List<IComparable>> rows = _interpreter.RunQueryAndSubqueries(query);

            Dictionary<int, string> indexToColumn = IndexToColumn(query, tableDef);

            List<T> transformedRows = new List<T>();

            foreach (var row in rows)
            {
                T t = new T();

                for (int i = 0; i < row.Count; i++)
                {
                    string columnName = indexToColumn[i];

                    TrySetProperty(t, columnName, row[i]);
                }

                transformedRows.Add(t);
            }

            return transformedRows;
        }

        private Dictionary<int, string> IndexToColumn(string query, TableDefinition tableDef)
        {
            Dictionary<int, string> selectIndexToColumnName = new Dictionary<int, string>();

            SelectParser selectParser = new SelectParser();

            List<SelectColumnDto> selectColumns = selectParser.GetColumns(query);

            if(selectColumns.First().ColumnName == "*")
            {
                return tableDef.ColumnDefinitions.ToDictionary(x => (int)x.Index, x => x.ColumnName);
            }

            for (int i = 0; i < selectColumns.Count; i++)
            {
                selectIndexToColumnName[i] = selectColumns[i].ColumnName;
            }

            return selectIndexToColumnName;
        }

        private bool TrySetProperty(object obj, string property, object value)
        {
            var prop = obj.GetType().GetProperty(property, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(obj, value, null);
                return true;
            }
            return false;
        }
    }
}
