using HotSauceDb.Enums;
using HotSauceDb.Models.Transactions;
using System;
using System.Collections.Generic;
using System.Text;

namespace HotSauceDb.Models
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
