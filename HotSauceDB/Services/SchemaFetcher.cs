using HotSauceDb.Models;
using System.Linq;

namespace HotSauceDb.Services
{
    public class SchemaFetcher
    {
        private IndexPage _indexPage;
        private Reader _reader;

        public SchemaFetcher(Reader reader)
        {
            _reader = reader;
        }

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

            IndexPage index = GetIndexPage();

            return index.TableDefinitions.Where(x => x.TableName == tableName).FirstOrDefault();
        }

    }
}
