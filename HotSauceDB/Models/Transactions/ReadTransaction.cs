using System.Collections.Generic;

namespace HotSauceDb.Models.Transactions
{
    public class ReadTransaction : UserTransaction
    {
        public IEnumerable<SelectColumnDto> Selects { get; set; }
        public List<PredicateOperation> PredicateOperations { get; set; }
    }
}
