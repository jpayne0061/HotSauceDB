﻿using HotSauceDb.Models;
using HotSauceDb.Services;
using HotSauceDB.Helpers;
using HotSauceDB.Statics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HotSauceDbOrm.Operations
{
    public class OperationsBase
    {
        protected Interpreter _interpreter;

        public OperationsBase(Interpreter interpreter)
        {
            _interpreter = interpreter;
        }
        public PropertyInfo GetIdentityColumn(object obj)
        {
            Type objectType = obj.GetType();

            return objectType.GetSupportedProperties().Where(x => x.IsIdentity(objectType)).FirstOrDefault();
        }

        protected IComparable[] GetRow(object obj)
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

            TableDefinition tableDef = _interpreter.GetTableDefinition(obj.GetType().Name);

            if(tableDef == null)
            {
                throw new Exception(ErrorMessages.NO_TABLE_FOUND(obj.GetType().Name));
            }

            PropertyInfo[] properties = OrderPropertiesByColumnIndex(tableDef, obj.GetType().GetSupportedProperties());

            int count = properties.Where(x => types.Contains(x.PropertyType)).Count();

            IComparable[] row = new IComparable[count];

            for (int i = 0; i < properties.Count(); i++)
            {
                if (properties[i].PropertyType == typeof(bool))
                {
                    row[i] = (bool)properties[i].GetValue(obj);
                }
                else if (properties[i].PropertyType == typeof(char))
                {
                    row[i] = (char)properties[i].GetValue(obj);
                }
                else if (properties[i].PropertyType == typeof(decimal))
                {
                    row[i] = (decimal)properties[i].GetValue(obj);
                }
                else if (properties[i].PropertyType == typeof(Int32))
                {
                    row[i] = (Int32)properties[i].GetValue(obj);
                }
                else if (properties[i].PropertyType == typeof(Int64))
                {
                    row[i] = (Int64)properties[i].GetValue(obj);
                }
                else if (properties[i].PropertyType == typeof(string))
                {
                    row[i] = (string)properties[i].GetValue(obj);
                }
                else if (properties[i].PropertyType == typeof(DateTime))
                {
                    row[i] = (DateTime)properties[i].GetValue(obj);
                }
            }

            return row;
        }

        protected PropertyInfo[] OrderPropertiesByColumnIndex(TableDefinition tableDefinition, PropertyInfo[] properties)
        {
            List<ColumnDefinition> columnDefinitions = tableDefinition.ColumnDefinitions;

            Dictionary<string, byte> columnNameToIndex = tableDefinition.ColumnDefinitions.ToDictionary(x => x.ColumnName, x => x.Index);

            properties = properties.OrderBy(x => columnNameToIndex[x.Name.ToLower()]).ToArray();

            return properties;
        }
    }
}
