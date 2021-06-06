using HotSauceDb.Enums;
using System.Collections.Generic;

namespace HotSauceDb.Models.Transactions
{
    public class InternalTransaction : BaseTransaction
    {
        public InternalTransactionType InternalTransactionType { get; set; }
        public List<object> Parameters { get; set; }
        public readonly string QueueKey = "disk";
    }
}
