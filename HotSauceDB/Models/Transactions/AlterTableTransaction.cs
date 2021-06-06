using HotSauceDb.Models;

namespace HotSauceDB.Models.Transactions
{
    public class AlterTableTransaction : UserTransaction
    {
        public ColumnDefinition NewColumn{ get; set; }
    }
}
