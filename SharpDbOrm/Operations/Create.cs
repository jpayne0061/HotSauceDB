using SharpDb.Enums;
using SharpDb.Models;
using SharpDb.Services;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SharpDbOrm.Operations
{
    public class Create
    {
        private Interpreter _interpreter;

        public Create(Interpreter interpreter)
        {
            _interpreter = interpreter;
        }

        public void CreateTable<T>(T model)
        {
            PropertyInfo[] properties = model.GetType().GetProperties();

            string name = model.GetType().Name;

            List<ColumnDefinition> columnDefinitions = GetColumnDefinitions(properties);

            TableDefinition tableDefinition = new TableDefinition
            {
                TableName = name,
                ColumnDefinitions = columnDefinitions
            };

            _interpreter.RunCreateTable(tableDefinition);
        }

        private List<ColumnDefinition> GetColumnDefinitions(PropertyInfo[] properties)
        {
            List<ColumnDefinition> colDefinitions = new List<ColumnDefinition>();

            for (int i = 0; i < properties.Length; i++)
            {
                ColumnDefinition columnDefinition = new ColumnDefinition();

                columnDefinition.ColumnName = properties[i].Name;
                columnDefinition.Index = (byte)i;
                columnDefinition.Type = GetTypeEnum(properties[i].PropertyType);

                if(columnDefinition.Type == TypeEnum.UnsupportedType)
                {
                    continue;
                }

                colDefinitions.Add(columnDefinition);
            }

            return colDefinitions;
        }

        private TypeEnum GetTypeEnum(Type type)
        {
            if (type == typeof(bool))
            {
                return TypeEnum.Boolean;
            }
            else if (type == typeof(char))
            {
                return TypeEnum.Char;
            }
            else if (type == typeof(decimal))
            {
                return TypeEnum.Decimal;
            }
            else if (type == typeof(Int32))
            {
                return TypeEnum.Int32;
            }
            else if (type == typeof(Int64))
            {
                return TypeEnum.Int64;
            }
            else if (type == typeof(string))
            {
                return TypeEnum.String;
            }
            else if (type == typeof(DateTime))
            {
                return TypeEnum.DateTime;
            }
            else
            {
                return TypeEnum.UnsupportedType;
            }
        }

    }
}
