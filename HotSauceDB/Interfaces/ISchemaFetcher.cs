using HotSauceDb.Models;

namespace HotSauceDB.Interfaces
{
    public interface ISchemaFetcher
    {
        void RefreshIndexPage();
        TableDefinition GetTableDefinition(string tableName);
    }
}
