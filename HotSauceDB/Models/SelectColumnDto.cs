using System;
using System.Collections.Generic;

namespace HotSauceDb.Models
{
    public class SelectColumnDto : ColumnDefinition
    {
        public SelectColumnDto() { }

        public SelectColumnDto(ColumnDefinition columnDefinition)
        {
            ColumnName = columnDefinition.ColumnName.ToLower();
            Index = columnDefinition.Index;
            Type = columnDefinition.Type;
            ByteSize = columnDefinition.ByteSize;
        }

        public bool IsInSelect { get; set; }
        public Func<List<IComparable>, IComparable> AggregateFunction { get; set; }

    }
}
