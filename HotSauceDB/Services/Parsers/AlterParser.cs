using HotSauceDb.Models;
using HotSauceDb.Services.Parsers;
using System.Collections.Generic;
using HotSauceDb.Helpers;

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

            ColumnDefinition columnDefinition = new ColumnDefinition
            {
                ColumnName = parts[4],
                Index = newColumnIndex
            };
            columnDefinition.Type = ParseTypeAndByteSize(parts[5].RemoveNewLines(), columnDefinition);

            return columnDefinition;
        }
    }
}
