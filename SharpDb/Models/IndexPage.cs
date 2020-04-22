using SharpDb.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SharpDb.Models
{
    public class IndexPage : ILocation
    {
        public IndexPage()
        {
            TableDefinitions = new List<TableDefinition>();
        }

        public long Address { get; set; } //set at zero
        public long MinPageId { get; set; } //8 bytes
        public long MaxPageId { get; set; } //8 bytes
        public long PageId { get; set; } //8 bytes
        public int NumberOfPagePointers { get; set; } //8 bytes
        public List<long> DataPageIds { get; set; }

        public List<TableDefinition> TableDefinitions { get; set; }
        public short NumberOfTableDefinitions { get; set; }

       

    }
}
