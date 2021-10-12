using HotSauceDb.Services;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace HotSauceDbOrm.Operations
{
    public class Read
    {
        private readonly Interpreter _interpreter;
        
        public Read(Interpreter interpreter)
        {
            _interpreter = interpreter;
        }

        public List<T> ReadRows<T>(string query) where T : new()
        {
            PropertyInfo[] properties = typeof(T).GetProperties();

            List<List<IComparable>> rows = _interpreter.RunQueryAndSubqueries(query);

            List<T> transformedRows = new List<T>();

            foreach (var row in rows)
            {
                T t = new T();

                for (int i = 0; i < row.Count; i++)
                {
                    TrySetProperty(t, properties[i].Name, row[i]);
                }

                transformedRows.Add(t);
            }

            return transformedRows;
        }

        private bool TrySetProperty(object obj, string property, object value)
        {
            var prop = obj.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(obj, value, null);
                return true;
            }
            return false;
        }
    }
}
