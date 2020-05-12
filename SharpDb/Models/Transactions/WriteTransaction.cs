using System;
using System.Collections.Generic;
using System.Text;

namespace SharpDb.Models.Transactions
{
    public class WriteTransaction : SharpDbTransaction
    {

        public IComparable[] Data { get; set; }
        public long AddressToWriteTo { get; set; }
    }
}
