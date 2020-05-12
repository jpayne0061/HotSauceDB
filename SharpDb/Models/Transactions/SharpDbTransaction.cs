using SharpDb.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpDb.Models
{
    public class SharpDbTransaction
    {
        public TableDefinition TableDefinition { get; set; }
        public string Key { get; set; }
        public string GetTableName {
            get
            {
                return TableDefinition.TableName.ToLower();
            }
        }
        public string Query { get; set; }
        protected virtual void OnTransactionComplete(EventArgs e)
        {
            EventHandler handler = TransactionComplete;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler TransactionComplete;
    }
}
