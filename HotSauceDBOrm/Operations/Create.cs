using HotSauceDb;
using HotSauceDb.Enums;
using HotSauceDb.Models;
using HotSauceDb.Services;
using HotSauceDB.Helpers;
using HotSauceDB.Services;
using HotSauceDB.Statics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace HotSauceDbOrm.Operations
{
    public class Create
    {
        private Interpreter           _interpreter;
        private SchemaComparer        _schemaComparer;
        private readonly DataMigrator _dataMigrator;

        public Create(Interpreter interpreter, 
                      SchemaComparer schemaComparer, 
                      DataMigrator   dataMigrator)
        {
            _interpreter =    interpreter;
            _schemaComparer = schemaComparer;
            _dataMigrator =   dataMigrator;
        }

        public void CreateTable<T>() where T : class, new()
        {
            PropertyInfo[] properties = typeof(T).GetProperties();

            string tableName = typeof(T).Name;

            bool tableAlreadyExists = TableAlreadyExists(tableName);

            if (tableAlreadyExists && _schemaComparer.TableDefinitionChanged<T>(tableName))
            {
                AlterTable<T>(tableName, properties);
            }
            else if(!tableAlreadyExists)
            {
                CreateTable<T>(tableName, properties);
            }
        }

        public bool TableAlreadyExists(string tableName)
        {
            if(File.ReadAllBytes(Constants.FILE_NAME).Length == 0)
            {
                return false;
            }

            var tableDefinition = _interpreter.GetTableDefinition(tableName);

            return tableDefinition != null;
        }

        private void AlterTable<T>(string tableName, PropertyInfo[] properties) where T : class, new()
        {
            TableDefinition newDefinition = CreateTable<T>(tableName, properties);

            _dataMigrator.MigrateData<T>(tableName, newDefinition);

            return;
        }

        private TableDefinition CreateTable<T>(string tableName, PropertyInfo[] properties) where T : class, new()
        {
            List<ColumnDefinition> columnDefinitions = CreateColumnDefinitions(tableName, properties);

            TableDefinition tableDefinition = new TableDefinition
            {
                TableName = tableName,
                ColumnDefinitions = columnDefinitions
            };

            _interpreter.RunCreateTable(tableDefinition);

            return tableDefinition;
        }

       
        private List<ColumnDefinition> CreateColumnDefinitions(string tableName, PropertyInfo[] properties)
        {
            List<ColumnDefinition> colDefinitions = new List<ColumnDefinition>();

            for (int i = 0; i < properties.Length; i++)
            {
                ColumnDefinition columnDefinition = new ColumnDefinition();

                columnDefinition.ColumnName = properties[i].Name;
                columnDefinition.Index = (byte)i;
                columnDefinition.Type = Constants.TypeToTypeEnum.GetValueIfKeyExists(properties[i].PropertyType);
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
                catch
                {
                    throw new Exception(ErrorMessages.String_Column_Attribute_Missing);
                }

                return (short)stringLength;
            }
            else
            {
                return 0;
            }
        }
    }
}
