using System;

namespace HotSauceDb.Models.Transactions
{
    public class BaseTransaction
    {
        public BaseTransaction()
        {
            DataRetrievalKey = Guid.NewGuid().ToString();
        }
        public string DataRetrievalKey { get; }
    }
}
