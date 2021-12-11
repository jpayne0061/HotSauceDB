using HotSauceDb;
using HotSauceDb.Enums;
using HotSauceDb.Models;
using HotSauceDb.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace HotSauceDB.Services
{
    public class SchemaComparer
    {
        private Interpreter _interpreter;

        public SchemaComparer(Interpreter interpreter)
        {
            _interpreter = interpreter;
        }

        public bool TableDefinitionChanged<T>(string tableName) where T : class, new()
        {
            PropertyInfo[] properties = typeof(T).GetProperties();

            TableDefinition tableDefinition = _interpreter.GetTableDefinition(tableName);

            var oldColumnTypeToDefinition = tableDefinition.ColumnDefinitions
                .ToDictionary(x => Tuple.Create(x.ColumnName.ToLower(), x.Type), x => x);

            Dictionary<string, ColumnDefinition> columnNameToDefinition =
                        tableDefinition.ColumnDefinitions.ToDictionary(x => x.ColumnName, x => x);

            if (properties.Length != tableDefinition.ColumnDefinitions.Count)
                return true;

            for (int i = 0; i < properties.Length; i++)
            {
                string newPropertyName = properties[i].Name;
                Type newPropertyType = properties[i].PropertyType;

                Tuple<string, TypeEnum> columnKey =
                    Tuple.Create(newPropertyName.ToLower(), Constants.TypeToTypeEnum[newPropertyType]);

                if (!oldColumnTypeToDefinition.ContainsKey(columnKey))
                    return true;

                ColumnDefinition columnDefinition = columnNameToDefinition[newPropertyName];
                TypeEnum typeEnum = Constants.TypeToTypeEnum[properties[i].PropertyType];

                if (typeEnum == TypeEnum.String && StringLengthAttributeChanged(properties[i], columnDefinition.ByteSize))
                    return true;
            }

            return false;
        }

        private bool StringLengthAttributeChanged(PropertyInfo propertyInfo, int existingStringLength)
        {
            StringLengthAttribute strLenAttr = propertyInfo.GetCustomAttributes(typeof(StringLengthAttribute), false)
                .Cast<StringLengthAttribute>().Single();

            return strLenAttr.MaximumLength != existingStringLength;
        }
    }
}
