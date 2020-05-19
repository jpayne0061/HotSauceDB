using System.Collections.Generic;

namespace SharpDb.Models
{
    public class IndexPage 
    {
        public IndexPage()
        {
            TableDefinitions = new List<TableDefinition>();
        }

        public long Address { get; set; } //set at zero

        public List<TableDefinition> TableDefinitions { get; set; }
    }
}
