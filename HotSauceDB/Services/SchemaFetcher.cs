using HotSauceDb.Models;
using HotSauceDB.Interfaces;
using System.Linq;

namespace HotSauceDb.Services
{
    public class SchemaFetcher : ISchemaFetcher
    {
        private IndexPage _indexPage;
        private readonly Reader _reader;

        public SchemaFetcher(Reader reader)
        {
            _reader = reader;
            _indexPage = _reader.GetIndexPage();
        }

        public void RefreshIndexPage()
        {
            _indexPage = _reader.GetIndexPage();
        }

        public TableDefinition GetTableDefinition(string tableName)
        {
            //TODO update table defintion in memory when adding table
            tableName = tableName.ToLower();

            return _indexPage.TableDefinitions.Where(x => x.TableName == tableName).FirstOrDefault();
        }

    }
}
