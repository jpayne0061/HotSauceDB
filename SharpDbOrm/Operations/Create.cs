using SharpDb;
using SharpDb.Enums;
using SharpDb.Models;
using SharpDb.Services;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public void CreateTable<T>()
        {
            PropertyInfo[] properties = typeof(T).GetProperties();

            string name = typeof(T).Name;

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
                columnDefinition.ByteSize = GetByteSize(columnDefinition.Type, properties[i]);

                if (columnDefinition.Type == TypeEnum.UnsupportedType)
                {
                    continue;
                }

                colDefinitions.Add(columnDefinition);
            }

            return colDefinitions;
        }

        private short GetByteSize(TypeEnum typeEnum, PropertyInfo propertyInfo = null)
        {
            if (typeEnum == TypeEnum.Boolean)
            {
                return Globals.BooleanByteLength;
            }
            else if (typeEnum == TypeEnum.Char)
            {
                return Globals.CharByteLength;
            }
            else if (typeEnum == TypeEnum.DateTime)
            {
                return Globals.Int64ByteLength;
            }
            else if (typeEnum == TypeEnum.Decimal)
            {
                return Globals.DecimalByteLength;
            }
            else if (typeEnum == TypeEnum.Int32)
            {
                return Globals.Int32ByteLength;
            }
            else if (typeEnum == TypeEnum.DateTime)
            {
                return Globals.Int64ByteLength;
            }
            else if (typeEnum == TypeEnum.String)
            {
                //get attribute from property
                int? stringLength = null;

                try
                {
                    stringLength = (int)propertyInfo.CustomAttributes.ToList()
                    .Where(x => x.AttributeType.Name == "StringLengthAttribute")
                    .First().ConstructorArguments.First().Value;
                }
                catch (Exception ex)
                {
                    throw new Exception(@"Failed to get string length from model. String properties
must have a StringLength attribute. Example:
[StringLength(20)] 
public string Name { get; set; }", ex);
                }

                return (short)stringLength;
            }
            else
            {
                return 0;
            }
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
