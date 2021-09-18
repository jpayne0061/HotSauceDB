using System.Collections.Generic;
using System.Linq;

namespace HotSauceDb.Models
{
    public class TableDefinition
    {
        string _tableName;
        bool _tableContainsIdentityColumn;

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

        public bool TableContainsIdentityColumn {
            get
            {
                if(ColumnDefinitions.Where(c => c.IsIdentity == 1).Any())
                {
                    return true;
                }

                return _tableContainsIdentityColumn;
            }
            set
            {
                _tableContainsIdentityColumn = value;
            }
        }
        public long TableDefinitionAddress { get; set; }

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
