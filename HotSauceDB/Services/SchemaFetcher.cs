using SharpDb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpDb.Services
{
    public class SchemaFetcher
    {
        public SchemaFetcher(Reader reader)
        {
            _reader = reader;
        }

        IndexPage _indexPage;
        private Reader _reader;

        public IndexPage GetIndexPage(bool overrideCache = true)
        {
            if(!overrideCache && _indexPage != null)
            {
                return _indexPage;
            }

            IndexPage indexPage = _reader.GetIndexPage();

            _indexPage = indexPage;

            return indexPage;
        }

        public TableDefinition GetTableDefinition(string tableName)
        {
            //TODO update table defintion in memory when adding table

            tableName = tableName.ToLower();

            var index = GetIndexPage();

            return index.TableDefinitions.Where(x => x.TableName == tableName).FirstOrDefault();
        }

    }
}
