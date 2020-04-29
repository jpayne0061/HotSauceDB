using System;
using System.Collections.Generic;
using System.Text;

namespace SharpDb.Models
{
    public class SelectColumnDto : ColumnDefinition
    {
        public SelectColumnDto(ColumnDefinition columnDefinition)
        {
            ColumnName = columnDefinition.ColumnName.ToLower();
            Index = columnDefinition.Index;
            Type = columnDefinition.Type;
            ByteSize = columnDefinition.ByteSize;
        }

        public bool IsInSelect { get; set; }
    }
}
