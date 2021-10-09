using HotSauceDb;
using HotSauceDb.Enums;
using HotSauceDb.Models;
using HotSauceDb.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace HotSauceDbOrm.Operations
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

            string tableName = typeof(T).Name;

            if(TableAlreadyExists(tableName))
            {
                return;
            }

            List<ColumnDefinition> columnDefinitions = GetColumnDefinitions(tableName, properties);

            TableDefinition tableDefinition = new TableDefinition
            {
                TableName = tableName,
                ColumnDefinitions = columnDefinitions
            };

            _interpreter.RunCreateTable(tableDefinition);
        }

        public bool TableAlreadyExists(string tableName)
        {
            if(File.ReadAllBytes(Constants.FILE_NAME).Length == 0)
            {
                return false;
            }

            return _interpreter.GetTableDefinition(tableName) != null;
        }

        private List<ColumnDefinition> GetColumnDefinitions(string tableName, PropertyInfo[] properties)
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

                if(columnDefinition.ColumnName.ToLower() == tableName.ToLower() + "id")
                {
                    columnDefinition.IsIdentity = 1;
                }

                colDefinitions.Add(columnDefinition);
            }

            return colDefinitions;
        }

        private short GetByteSize(TypeEnum typeEnum, PropertyInfo propertyInfo = null)
        {
            if (typeEnum == TypeEnum.Boolean)
            {
                return Constants.Boolean_Byte_Length;
            }
            else if (typeEnum == TypeEnum.Char)
            {
                return Constants.Char_Byte_Length;
            }
            else if (typeEnum == TypeEnum.DateTime)
            {
                return Constants.Int64_Byte_Length;
            }
            else if (typeEnum == TypeEnum.Decimal)
            {
                return Constants.Decimal_Byte_Length;
            }
            else if (typeEnum == TypeEnum.Int32)
            {
                return Constants.Int32_Byte_Length;
            }
            else if (typeEnum == TypeEnum.DateTime)
            {
                return Constants.Int64_Byte_Length;
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
