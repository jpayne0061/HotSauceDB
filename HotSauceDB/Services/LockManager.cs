using HotSauceDB.Models;
using HotSauceDb.Enums;
using HotSauceDb.Models;
using HotSauceDb.Models.Transactions;
using System;
using System.Collections.Concurrent;
using HotSauceDB.Models.Transactions;

namespace HotSauceDb.Services
{
    public class LockManager
    {
        private readonly Writer _writer;
        private readonly Reader _reader;
        private ConcurrentDictionary<long, object> _tableLocks;
        private IndexPage _indexPage;

        public LockManager(Writer writer, Reader reader)
        {
            _writer = writer;
            _reader = reader;
            _tableLocks = new ConcurrentDictionary<long, object>();
            SetupTableLocks();
        }

        public SelectData ProcessReadTransaction(ReadTransaction readTransaction)
        {
            lock(_tableLocks[readTransaction.TableDefinition.DataAddress])
            {
                SelectData selectData = _reader.GetRows(readTransaction.TableDefinition, readTransaction.Selects, readTransaction.PredicateOperations);

                return selectData;
            }
        }

        public InsertResult ProcessWriteTransaction(WriteTransaction writeTransaction)
        {
            lock (_tableLocks[writeTransaction.TableDefinition.DataAddress])
            {
                _writer.WriteRow(writeTransaction.Data, writeTransaction.TableDefinition, writeTransaction.AddressToWriteTo, writeTransaction.UpdateObjectCount);

                return new InsertResult { Successful = true };
            }
        }

        public ResultMessage ProcessCreateTableTransaction(SchemaTransaction schemaTransaction)
        {
            ResultMessage msg = _writer.WriteTableDefinition(schemaTransaction.TableDefinition);

            _tableLocks[msg.Data] = new object();

            return msg;
        }

        public ResultMessage ProcessAlterTableTransaction(AlterTableTransaction alterTableTransaction)
        {
            lock (_tableLocks[alterTableTransaction.TableDefinition.DataAddress])
            {
                return _writer.AlterTableDefinition(alterTableTransaction.TableDefinition, alterTableTransaction.NewColumn);
            }
        }

        public long ProcessInternalTransaction(InternalTransaction internalTransaction)
        {
            switch (internalTransaction.InternalTransactionType)
            {
                case InternalTransactionType.GetFirstAvailableDataAddress:
                    long address = _reader.GetFirstAvailableDataAddress((long)internalTransaction.Parameters[0], (int)internalTransaction.Parameters[1]);
                    return address;
                default:
                    throw new Exception("invalid internal transaction type ");
            }
        }

        private void SetupTableLocks()
        {
            if(!_reader.DatabaseEmpty())
            {
                IndexPage indexPage = _reader.GetIndexPage();

                foreach (var table in indexPage.TableDefinitions)
                {
                    _tableLocks[table.DataAddress] = new object();
                }
            }
        }
    }
}
