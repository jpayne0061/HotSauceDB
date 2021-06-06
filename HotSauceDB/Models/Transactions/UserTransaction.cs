using HotSauceDb.Models.Transactions;

namespace HotSauceDb.Models
{
    public class UserTransaction : BaseTransaction
    {
        public TableDefinition TableDefinition { get; set; }
        public string GetTableName {
            get
            {
                return TableDefinition.TableName.ToLower();
            }
        }
        public string Query { get; set; }
    }
}
