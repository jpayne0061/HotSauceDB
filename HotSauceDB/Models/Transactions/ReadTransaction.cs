using System;
using System.Collections.Generic;
using System.Text;

namespace SharpDb.Models.Transactions
{
    public class ReadTransaction : UserTransaction
    {
        public IEnumerable<SelectColumnDto> Selects { get; set; }

        public List<PredicateOperation> PredicateOperations { get; set; }

    }
}
