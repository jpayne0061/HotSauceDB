using SharpDb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpDb.Services
{
    public class SchemaFetcher
    {
        IndexPage _indexPage;

        public IndexPage GetIndexPage(bool overrideCache = true)
        {
            if(!overrideCache && _indexPage != null)
            {
                return _indexPage;
            }

            var reader = new Reader();

            IndexPage indexPage = reader.GetIndexPage();

            _indexPage = indexPage;

            return indexPage;
        }

        public TableDefinition GetTableDefinition(string tableName)
        {
            //TODO update table defintion in memory when adding table

            tableName = tableName.ToLower();

            var index = GetIndexPage();

            return index.TableDefinitions.Where(x => x.TableName == tableName).First();
        }

    }
}
