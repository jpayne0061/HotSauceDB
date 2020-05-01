using SharpDb.Interfaces;
using SharpDb.Services;
using System;
using System.Collections.Generic;
using System.IO;



namespace SharpDb.Models
{
    public class DataPage : ILocation
    {
        public DataPage()
        {
            TableDefinition = new TableDefinition();
            TableDefinition.ColumnDefinitions = new List<ColumnDefinition>();
        }

        public long Address { get; set; }
        public int NumberOfRows { get; set; } //2 bytes
        public int RowSize { get; set; } // 2 bytes
        //public int NumColumnDefinitions { get; set; } //2 bytes
        public TableDefinition TableDefinition { get; set; } //400 bytes (hold up to 100 columns)

    }
}
