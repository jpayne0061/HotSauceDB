﻿using HotSauceDb.Models;
using HotSauceDB.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HotSauceDb.Services.Parsers
{
    public class InsertParser : GeneralParser
    {
        readonly ISchemaFetcher _schemaFetcher;

        public InsertParser(ISchemaFetcher schemaFetcher)
        {
            _schemaFetcher = schemaFetcher;
        }

        public string ParseTableName(string dml)
        {
            dml = ToLowerAndTrim(dml);

            List<string> dmlParts = dml.Split(' ')
                .Where(x => !string.IsNullOrEmpty(x) && !string.IsNullOrWhiteSpace(x))
                .ToList();

            if(dmlParts[0] != "insert" || dmlParts[1] != "into" || dmlParts[3] != "values")
            {
                throw new Exception($"invalid insert statement {dml}");
            }

            return dmlParts[2];
        }

        public bool IsValidStatement()
        {
            return false;
        }

        public IComparable[] GetRow(string csv, TableDefinition tableDefinition)
        {
            StringParser converter = new StringParser();

            List<string> vals = csv.Split(',').Select(x => x.Trim()).ToList();

            if(vals.Count != tableDefinition.ColumnDefinitions.Count - (tableDefinition.TableContainsIdentityColumn ? 1 : 0))
            {
                throw new Exception($"Table '{tableDefinition.TableName}' requires {tableDefinition.ColumnDefinitions.Count} values for an insert, but {vals.Count} was provided: {string.Join(',', vals)}");
            }

            List<ColumnDefinition> nonIdentityColumns = tableDefinition.ColumnDefinitions.Where(c => c.IsIdentity != 1).ToList();

            IComparable[] comparables = new IComparable[nonIdentityColumns.Count];

            for (int i = 0; i < nonIdentityColumns.Count(); i++)
            {
                comparables[i] = converter.ConvertToType(vals[i], nonIdentityColumns[i].Type);
            }

            return comparables;
        }

        public IComparable[] GetRow(string dml)
        {
            string tableName = ParseTableName(dml);

            TableDefinition tableDefinition = _schemaFetcher.GetTableDefinition(tableName);

            InnerStatement innerStatementValues = GetFirstMostInnerParantheses(dml);

            IComparable[] comparables = GetRow(innerStatementValues.Statement, tableDefinition);

            return comparables;
        }


    }
}
