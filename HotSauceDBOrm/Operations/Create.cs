using HotSauceDb;
using HotSauceDb.Enums;
using HotSauceDb.Models;
using HotSauceDb.Services;
using HotSauceDB.Helpers;
using HotSauceDB.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

        public void CreateTable<T>() where T : class, new()
        {
            PropertyInfo[] properties = typeof(T).GetProperties();

            string tableName = typeof(T).Name;

            if(TableAlreadyExists(tableName))
            {
                AlterTableIfTableDefinitionChanged<T>(tableName);
                return;
            }

            CreateTable<T>(tableName, properties);
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

        private TableDefinition CreateTable<T>(string tableName, PropertyInfo[] properties) where T : class, new()
        {
            List<ColumnDefinition> columnDefinitions = GetColumnDefinitions(tableName, properties);

            TableDefinition tableDefinition = new TableDefinition
            {
                TableName = tableName,
                ColumnDefinitions = columnDefinitions
            };

            _interpreter.RunCreateTable(tableDefinition);

            return tableDefinition;
        }

        private void AlterTableIfTableDefinitionChanged<T>(string tableName) where T : class, new()
        {
            PropertyInfo[] properties = typeof(T).GetProperties();

            TableDefinition tableDefinition = _interpreter.GetTableDefinition(tableName);

            if(!ColumnDefinitionsChanged(properties, tableDefinition.ColumnDefinitions))
            {
                return;
            }

            TableDefinition newDefinition = CreateTable<T>(tableName, properties);

            var oldRows = _interpreter.RunQuery($"select * from {tableName}");

            tableDefinition.TableName = new string(Guid.NewGuid().ToString().Take(tableName.Count()).ToArray());
            _interpreter.RenameTable(tableDefinition);

            var newTableData = TransformOldTableDataToNewTableData(oldRows, newDefinition.ColumnDefinitions, tableDefinition.ColumnDefinitions);

            foreach (var row in newTableData)
            {
                _interpreter.RunInsert(row, tableName);
            }
        }

        private List<IComparable[]> TransformOldTableDataToNewTableData(List<List<IComparable>> oldRows,
                                                                    List<ColumnDefinition> newColumnDefinitions,
                                                                    List<ColumnDefinition> oldColumnDefinitions)
        {
            var dataSignatureToDefinitionNew = newColumnDefinitions.ToDictionary(x => Tuple.Create(x.ColumnName.ToLower(), x.Type), x => x);

            var indexToNewColumnDefinition = newColumnDefinitions.ToDictionary(x => x.Index, x => x);

            var indexToDefinitionOld = oldColumnDefinitions.ToDictionary(x => x.Index, x => x);

            List<IComparable[]> newRows = new List<IComparable[]>();

            for (int i = 0; i < oldRows.Count; i++)
            {
                IComparable[] newRow = new IComparable[newColumnDefinitions.Count];

                var populatedIndexes = new HashSet<byte>();
                var nonPopulatedIndexes = new HashSet<byte>();

                //fill in values for columns that exist in both
                for (byte j = 0; j < oldRows[i].Count; j++)
                {
                    ColumnDefinition oldColDefinition = indexToDefinitionOld[j];

                    Tuple<string, TypeEnum> columnKey = Tuple.Create(oldColDefinition.ColumnName.ToLower(), oldColDefinition.Type);

                    if (dataSignatureToDefinitionNew.ContainsKey(columnKey))
                    {
                        byte newColumnIndex = dataSignatureToDefinitionNew[columnKey].Index;
                        newRow[newColumnIndex] = oldRows[i][j];
                        populatedIndexes.Add(newColumnIndex);
                    }
                }

                for (byte j = 0; j < newRow.Length; j++)
                {
                    if(!populatedIndexes.Contains(j))
                    {
                        IComparable val = DefaultValues.GetDefaultDotNetValueForType(indexToNewColumnDefinition[j].Type);
                        newRow[j] = val;
                    }
                }

                newRows.Add(newRow);
            }

            return newRows;
        }


        private bool ColumnDefinitionsChanged(PropertyInfo[] properties, List<ColumnDefinition> columnDefinitions)
        {
            Dictionary<string, ColumnDefinition> columnNameToDefinition = columnDefinitions.ToDictionary(x => x.ColumnName, x => x);

            Dictionary<Type, TypeEnum> typeToTypeEnum = new Dictionary<Type, TypeEnum>();
            typeToTypeEnum[typeof(bool)]     = TypeEnum.Boolean;
            typeToTypeEnum[typeof(char)]     = TypeEnum.Char;
            typeToTypeEnum[typeof(DateTime)] = TypeEnum.DateTime;
            typeToTypeEnum[typeof(decimal)]  = TypeEnum.Decimal;
            typeToTypeEnum[typeof(Int32)]    = TypeEnum.Int32;
            typeToTypeEnum[typeof(Int64)]    = TypeEnum.Int64;
            typeToTypeEnum[typeof(string)]   = TypeEnum.String;


            if (properties.Length != columnDefinitions.Count)
            {
                return true;
            }

            for (int i = 0; i < properties.Length; i++)
            {
                ColumnDefinition columnDefinition = null;
                TypeEnum typeEnum = 0;

                try
                {
                    columnDefinition = columnNameToDefinition[properties[i].Name.ToLower()];
                    typeEnum = typeToTypeEnum[properties[i].PropertyType];

                    if(typeEnum == TypeEnum.String && StringLengthAttributeChanged(properties[i], columnDefinition.ByteSize))
                    {
                        return true;
                    }

                }
                catch (KeyNotFoundException)
                {
                    return true;
                }
            }

            return false;
        }

        private bool StringLengthAttributeChanged(PropertyInfo propertyInfo, int existingStringLength)
        {
            StringLengthAttribute strLenAttr = propertyInfo.GetCustomAttributes(typeof(StringLengthAttribute), false)
                .Cast<StringLengthAttribute>().Single();

            return strLenAttr.MaximumLength != existingStringLength;
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
