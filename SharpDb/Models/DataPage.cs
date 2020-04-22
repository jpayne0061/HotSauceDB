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
        
        public void WriteRow(object[] row, long diskLocation, TableDefinition table)
        {
            var writer = new Writer();

            using (FileStream fileStream = File.OpenWrite(Globals.FILE_NAME))
            {
                fileStream.Position = diskLocation;// + Globals.TABLE_DEF_LENGTH;

                var rowsSizeInBytes = NumberOfRows * TableDefinition.GetRowSizeInBytes();

                using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
                {
                    for(var i = 0; i < row.Length; i ++)
                    {
                        writer.WriteColumnData(binaryWriter, row[i], table.ColumnDefinitions[i]);
                    }
                    NumberOfRows += 1;
                }
            }
        }

    }
}
