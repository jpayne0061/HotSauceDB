using SharpDb.Enums;
using SharpDb.Models.Transactions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpDb.Models
{
    public class UserTransaction : BaseTransaction
    {
        public TableDefinition TableDefinition { get; set; }
        public string GetTableName {
            get
            {
                return TableDefinition.TableName.ToLower();
            }
        }
        public string Query { get; set; }
    }
}
