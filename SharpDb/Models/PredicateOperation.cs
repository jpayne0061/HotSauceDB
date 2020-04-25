using System;
using System.Collections.Generic;
using System.Text;

namespace SharpDb.Models
{
    public class PredicateOperation
    {
        public Func<IComparable, IComparable, bool> Delegate { get; set; }
        public string Predicate { get; set; }
        public string ColumnName { get; set; }
        public int ColumnIndex { get; set; }
        public IComparable Value { get; set; }
        public string Operator { get; set; }
    }
}
