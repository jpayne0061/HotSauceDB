using HotSauceDb.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace HotSauceDb.Models.Transactions
{
    public class InternalTransaction : BaseTransaction
    {
        public InternalTransactionType InternalTransactionType { get; set; }
        public List<object> Parameters { get; set; }
        public readonly string QueueKey = "disk";
    }
}
