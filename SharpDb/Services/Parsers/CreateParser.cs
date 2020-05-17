using SharpDb.Enums;
using SharpDb.Helpers;
using SharpDb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpDb.Services.Parsers
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

            if(columnParts.Length > 100)
            {
                throw new Exception("Column definitions exceed max count of 100. Only 100 columns allowed per table");
            }

            List<ColumnDefinition> colDefinitions = new List<ColumnDefinition>();

            for (int i = 0; i < columnParts.Length; i++)
            {
                string[] columnNameAndType = columnParts[i].Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

                ColumnDefinition columnDefinition = new ColumnDefinition();

                columnDefinition.ColumnName = columnNameAndType[0].RemoveNewLines();
                columnDefinition.Index = (byte)i;
                columnDefinition.Type = ParseTypeAndByteSize(columnNameAndType[1].RemoveNewLines(), columnDefinition);

                colDefinitions.Add(columnDefinition);
            }

            return colDefinitions;

        }

        //refactor
        //do not manipulate object (columnDefinition) while also returning a value - seems ugly
        private TypeEnum ParseTypeAndByteSize(string type, ColumnDefinition colDef)
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
                    colDef.ByteSize = Globals.DecimalByteLength;
                    return TypeEnum.Decimal;
                case "bool":
                    colDef.ByteSize = Globals.BooleanByteLength;
                    return TypeEnum.Boolean;
                case "char":
                    colDef.ByteSize = Globals.CharByteLength;
                    return TypeEnum.Char;
                case "int":
                    colDef.ByteSize = Globals.Int32ByteLength;
                    return TypeEnum.Int32;
                case "bigint":
                    colDef.ByteSize = Globals.Int64ByteLength;
                    return TypeEnum.Int64;
                case "datetime":
                    colDef.ByteSize = Globals.Int64ByteLength;
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

        public string GetTableName(string dml)
        {
            dml = ToLowerAndTrim(dml);

            string[] parts = dml.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            string tableName = parts[2];

            tableName = tableName.Split('(')[0];

            return tableName;
        }

    }
}
