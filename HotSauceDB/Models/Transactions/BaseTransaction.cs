using System;
using System.Collections.Generic;
using System.Text;

namespace SharpDb.Models.Transactions
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
