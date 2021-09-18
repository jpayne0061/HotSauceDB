using System;

namespace HotSauceDb.Models
{
    public class InsertResult : OperationResult {
        public IComparable IdentityValue { get; set; }
    }

}
