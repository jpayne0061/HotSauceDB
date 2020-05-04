using System;
using System.Collections.Generic;
using System.Text;

namespace SharpDb.Models
{
    public class PredicateOperation
    {
        public Func<IComparable, object, bool> Delegate { get; set; }
        public string Predicate { get; set; }
        public string ColumnName { get; set; }
        public int ColumnIndex { get; set; }
        public object Value { get; set; }
        public string Operator { get; set; }
    }
}
