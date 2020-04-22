using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpDb.Models
{
    public class TableDefinition
    {
        public TableDefinition()
        {
            ColumnDefinitions = new List<ColumnDefinition>();
        }
        public List<ColumnDefinition> ColumnDefinitions { get; set; }
        public string TableName { get; set; } //41 bytes - string length 20
        public long DataAddress { get; set; } //8 bytes
        public int GetRowSizeInBytes()
        {
            int byteSize = 0;

            foreach (var item in ColumnDefinitions)
            {
                byteSize += item.ByteSize;
            }

            return byteSize;
        }
    }
}
