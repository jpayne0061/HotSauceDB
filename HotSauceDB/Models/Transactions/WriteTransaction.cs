using System;

namespace HotSauceDb.Models.Transactions
{
    public class WriteTransaction : UserTransaction
    {
        public IComparable[] Data { get; set; }
        public long AddressToWriteTo { get; set; }
        public bool UpdateObjectCount = true;
    }
}
