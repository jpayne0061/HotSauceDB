using System;

namespace HotSauceDb.Models
{
    public class PredicateOperation
    {
        public Func<IComparable, object, bool> Delegate { get; set; }
        public int ColumnIndex { get; set; }
        public object Value { get; set; }
        public string Operator { get; set; }
    }
}
