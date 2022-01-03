using HotSauceDB.Models;
using HotSauceDb.Models;
using HotSauceDb.Models.Transactions;
using System;
using System.Collections.Concurrent;

namespace HotSauceDb.Services
{
    public class LockManager
    {
        private readonly Writer _writer;
        private readonly Reader _reader;
        private readonly ConcurrentDictionary<long, object> _tableLocks;

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
                IComparable identity = _writer.WriteRow(writeTransaction.Data, writeTransaction.TableDefinition, writeTransaction.AddressToWriteTo, writeTransaction.UpdateObjectCount);

                return new InsertResult { Successful = true, IdentityValue = identity };
            }
        }

        public ResultMessage ProcessCreateTableTransaction(SchemaTransaction schemaTransaction)
        {
            ResultMessage msg = _writer.WriteTableDefinition(schemaTransaction.TableDefinition);

            _tableLocks[msg.Data] = new object();

            return msg;
        }

        public ResultMessage RenameTable(SchemaTransaction schemaTransaction)
        {
            ResultMessage msg = _writer.RenameTable(schemaTransaction.TableDefinition);

            _tableLocks[msg.Data] = new object();

            return msg;
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
