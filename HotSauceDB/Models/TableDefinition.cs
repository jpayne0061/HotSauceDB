using System.Collections.Generic;

namespace HotSauceDb.Models
{
    public class TableDefinition
    {
        string _tableName;

        public TableDefinition()
        {
            ColumnDefinitions = new List<ColumnDefinition>();
        }

        public TableDefinition(TableDefinition tableDefinition)
        {
            ColumnDefinitions = new List<ColumnDefinition>();

            for (int i = 0; i < tableDefinition.ColumnDefinitions.Count; i++)
            {
                ColumnDefinitions.Add(tableDefinition.ColumnDefinitions[i]);
            }
            TableName = tableDefinition.TableName;
            DataAddress = tableDefinition.DataAddress;
            TableDefinitionAddress = tableDefinition.TableDefinitionAddress;
        }

        public List<ColumnDefinition> ColumnDefinitions { get; set; }
        public string TableName //41 bytes - string length 20
        {
            get
            {
                return _tableName.ToLower();
            }
            set
            {
                _tableName = value;
            }
        }
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
        public long TableDefinitionAddress { get; set; }
    }
}
