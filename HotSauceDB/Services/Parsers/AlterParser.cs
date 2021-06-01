using SharpDb.Models;
using SharpDb.Services.Parsers;
using System;
using System.Collections.Generic;
using SharpDb.Helpers;

namespace HotSauceDB.Services.Parsers
{
    public class AlterParser : CreateParser
    {
        public new string GetTableName(string sql)
        {
            sql = ToLowerAndTrim(sql);

            List<string> parts = SplitOnSeparatorsExceptQuotesAndParantheses(sql, new char[] { ' ', '\r', '\n' });

            return parts[2];
        }

        public ColumnDefinition GetNewColumnDefinition(string sql, byte newColumnIndex)
        {
            sql = ToLowerAndTrim(sql);

            List<string> parts = SplitOnSeparatorsExceptQuotesAndParantheses(sql, new char[] { ' ', '\r', '\n' });

            ColumnDefinition columnDefinition = new ColumnDefinition();

            columnDefinition.ColumnName = parts[4];
            columnDefinition.Index = newColumnIndex;
            columnDefinition.Type = ParseTypeAndByteSize(parts[5].RemoveNewLines(), columnDefinition);

            return columnDefinition;
        }
    }
}
