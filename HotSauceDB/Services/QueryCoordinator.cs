using HotSauceDB.Models;
using SharpDb.Enums;
using SharpDb.Models;
using SharpDb.Models.Transactions;
using System;
using System.Collections.Concurrent;

namespace SharpDb.Services
{
    public class QueryCoordinator
    {
        private readonly Writer _writer;
        private readonly Reader _reader;

        public QueryCoordinator(Writer writer, Reader reader)
        {
            _writer = writer;
            _reader = reader;
        }

        public ConcurrentDictionary<string, ConcurrentQueue<BaseTransaction>> _queues
            = new ConcurrentDictionary<string, ConcurrentQueue<BaseTransaction>>();

        public ConcurrentDictionary<string, object> DataStore = new ConcurrentDictionary<string, object>();

        public void QueueQuery(UserTransaction sharpDbTransaction)
        {
            string tableName = sharpDbTransaction.GetTableName;

            var q = _queues.GetOrAdd(tableName, x => new ConcurrentQueue<BaseTransaction>());

            q.Enqueue(sharpDbTransaction);

            ProcessNextQueueItems();
        }

        public void QueueInternalTransaction(InternalTransaction transaction)
        {
            var q = _queues.GetOrAdd(transaction.QueueKey, x => new ConcurrentQueue<BaseTransaction>());

            q.Enqueue(transaction);

            ProcessNextQueueItems();
        }

        public void ProcessNextQueueItems()
        {
            bool allQueuesEmpty = true;

            foreach(var queue in _queues)
            {
                if (queue.Value.IsEmpty)
                {
                    continue;
                }
                else
                {
                    allQueuesEmpty = false;
                }

                BaseTransaction sharpDbTransaction;

                bool succeeded = queue.Value.TryDequeue(out sharpDbTransaction);

                if (succeeded)
                {
                    if (sharpDbTransaction is WriteTransaction)
                    {
                        WriteTransaction writeTxn = (WriteTransaction)sharpDbTransaction;

                        _writer.WriteRow(writeTxn.Data, writeTxn.TableDefinition, writeTxn.AddressToWriteTo, writeTxn.UpdateObjectCount);

                        DataStore[sharpDbTransaction.DataRetrievalKey] = new InsertResult { Successful = true };
                    }
                    else if (sharpDbTransaction is ReadTransaction)
                    {
                        ReadTransaction readTransaction = (ReadTransaction)sharpDbTransaction;

                        SelectData selectData = _reader.GetRows(readTransaction.TableDefinition, readTransaction.Selects, readTransaction.PredicateOperations);

                        DataStore[sharpDbTransaction.DataRetrievalKey] = selectData;
                    }
                    else if(sharpDbTransaction is SchemaTransaction)
                    {
                        SchemaTransaction dmlTransaction = (SchemaTransaction)sharpDbTransaction;

                        DataStore[sharpDbTransaction.DataRetrievalKey] = _writer.WriteTableDefinition(dmlTransaction.TableDefinition);
                    }
                    else if (sharpDbTransaction is InternalTransaction)
                    {
                        InternalTransaction txn = (InternalTransaction)sharpDbTransaction;

                        switch(txn.InternalTransactionType)
                        {
                            case (InternalTransactionType.GetFirstAvailableDataAddress):
                                long address =  _reader.GetFirstAvailableDataAddress((long)txn.Parameters[0], (int)txn.Parameters[1]);
                                DataStore[txn.DataRetrievalKey] = address;
                                break;
                            default:
                                throw new Exception("invalid internal transaction type ");
                        }
                    }
                }
            }

            if(!allQueuesEmpty)
            {
                ProcessNextQueueItems();
            }
        }
    }
}
