using System;
using System.Collections.Generic;
using System.Text;

namespace SharpDb.Models
{
    public class Aggregate
    {
        public string ColumnName { get; set; }
        public Func<List<IComparable>, IComparable> AggregateFunction { get; set; }
    }
}
