using SharpDb.Enums;
using SharpDb.Models;
using SharpDb.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharpDbOrm.Operations
{
    public class Insert
    {
        private Interpreter _interepreter;

        public Insert(Interpreter interepreter)
        {
            _interepreter = interepreter;
        }

        public void InsertRow<T>(T model)
        {
            IComparable[] row = GetRow(model);

            string tableName = model.GetType().Name;

            _interepreter.RunInsert(row, tableName);
        }

        private IComparable[] GetRow<T>(T model)
        {
            HashSet<Type> types = new HashSet<Type>
            {
                typeof(bool),
                typeof(char),
                typeof(decimal),
                typeof(Int32),
                typeof(Int64),
                typeof(string),
                typeof(DateTime)
            };

            PropertyInfo[] properties = model.GetType().GetProperties();

            int count = properties.Where(x => types.Contains(x.PropertyType)).Count();

            IComparable[] row = new IComparable[count];

            for (int i = 0; i < properties.Count(); i++)
            {
                if (properties[i].PropertyType == typeof(bool))
                {
                    row[i] = (bool)properties[i].GetValue(model);
                }
                else if (properties[i].PropertyType == typeof(char))
                {
                    row[i] = (char)properties[i].GetValue(model);
                }
                else if (properties[i].PropertyType == typeof(decimal))
                {
                    row[i] = (decimal)properties[i].GetValue(model);
                }
                else if (properties[i].PropertyType == typeof(Int32))
                {
                    row[i] = (Int32)properties[i].GetValue(model);
                }
                else if (properties[i].PropertyType == typeof(Int64))
                {
                    row[i] = (Int64)properties[i].GetValue(model);
                }
                else if (properties[i].PropertyType == typeof(string))
                {
                    row[i] = (string)properties[i].GetValue(model);
                }
                else if (properties[i].PropertyType == typeof(DateTime))
                {
                    row[i] = (DateTime)properties[i].GetValue(model);
                }
            }


            return row;
        }
    }
}
