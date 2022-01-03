using HotSauceDb.Enums;
using HotSauceDb.Helpers;
using HotSauceDb.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HotSauceDb.Services.Parsers
{
    public class CreateParser : GeneralParser
    {
        public List<ColumnDefinition> GetColumnDefintions(string dml)
        {
            dml = ToLowerAndTrim(dml);

            string[] parts = dml.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            string tableName = parts[2];

            InnerStatement columnDefinitionStatement = GetOuterMostParantheses(dml);

            string[] columnParts = columnDefinitionStatement.Statement.Split(',').Where( x => !string.IsNullOrWhiteSpace(x)).ToArray();

            if(columnParts.Length > Constants.Max_Columns)
            {
                throw new Exception("Column definitions exceed max count of 100. Only 100 columns allowed per table");
            }

            List<ColumnDefinition> colDefinitions = new List<ColumnDefinition>();

            for (int i = 0; i < columnParts.Length; i++)
            {
                string[] columnNameAndType = columnParts[i].Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

                if(columnNameAndType.Length < 2)
                {
                    throw new Exception($"Column name or data type missing from table definition: '{columnNameAndType[0]}'");
                }

                bool isIdentityColumn = IsIdentityColumn(columnNameAndType);

                if (isIdentityColumn && i > 0)
                {
                    throw new Exception("Identity column must be first column in table definition");
                }

                ColumnDefinition columnDefinition = new ColumnDefinition
                {
                    ColumnName = columnNameAndType[0].RemoveNewLines(),
                    Index = (byte)i
                };
                columnDefinition.Type = ParseTypeAndByteSize(columnNameAndType[1].RemoveNewLines(), columnDefinition);
                columnDefinition.IsIdentity = isIdentityColumn ? (byte)1 : (byte)0;

                colDefinitions.Add(columnDefinition);
            }

            return colDefinitions;

        }

        private bool IsIdentityColumn(string[] columnPart)
        {
            return columnPart.Length == 3 && columnPart[2].ToLower() == Constants.IDENTITY_MARKER;
        }

        public string GetTableName(string dml)
        {
            dml = ToLowerAndTrim(dml);

            string[] parts = dml.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            string tableName = parts[2];

            tableName = tableName.Split('(')[0];

            return tableName;
        }

        //refactor
        //do not manipulate object (columnDefinition) while also returning a value - seems ugly
        protected TypeEnum ParseTypeAndByteSize(string type, ColumnDefinition colDef)
        {
            if(type.Length > 6 && type.Substring(0, 7) == "varchar")
            {
                colDef.ByteSize = ParseVarcharSize(type);
                colDef.ByteSize += 1;
                return TypeEnum.String;
            }

            switch(type)
            {
                case "decimal":
                    colDef.ByteSize = Constants.Decimal_Byte_Length;
                    return TypeEnum.Decimal;
                case "bool":
                    colDef.ByteSize = Constants.Boolean_Byte_Length;
                    return TypeEnum.Boolean;
                case "char":
                    colDef.ByteSize = Constants.Char_Byte_Length;
                    return TypeEnum.Char;
                case "int":
                    colDef.ByteSize = Constants.Int32_Byte_Length;
                    return TypeEnum.Int32;
                case "bigint":
                    colDef.ByteSize = Constants.Int64_Byte_Length;
                    return TypeEnum.Int64;
                case "datetime":
                    colDef.ByteSize = Constants.Int64_Byte_Length;
                    return TypeEnum.DateTime;
                default:
                    throw new Exception($"{type} is not recognized as a valid type");

            }
        }

        private short ParseVarcharSize(string varchar)
        {
            string num = GetOuterMostParantheses(varchar).Statement;

            return short.Parse(num);
        }
    }
}
