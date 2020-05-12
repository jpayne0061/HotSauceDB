using SharpDb.Helpers;
using SharpDb.Models;
using SharpDb.Models.Transactions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SharpDb.Services
{
    public class LockManager
    {
        private readonly Writer _writer;
        private readonly Reader _reader;

        public LockManager(Writer writer, Reader reader)
        {
            _writer = writer;
            _reader = reader;
        }

        public ConcurrentDictionary<string, ConcurrentQueue<SharpDbTransaction>> _queues
            = new ConcurrentDictionary<string, ConcurrentQueue<SharpDbTransaction>>();

        public Dictionary<string, object> DataStore = new Dictionary<string, object>();

        public void Queue(object sharpDbTransaction)
        {
            var castedSharpDbTransaction = (SharpDbTransaction)sharpDbTransaction;

            string tableName = castedSharpDbTransaction.GetTableName;

            if (_queues.ContainsKey(tableName))
            {
                _queues[tableName].Enqueue(castedSharpDbTransaction);
            }
            else
            {
                _queues[tableName] = new ConcurrentQueue<SharpDbTransaction>();
                _queues[tableName].Enqueue(castedSharpDbTransaction);
            }

            ProcessNextQueueItems();
        }

        public void ProcessNextQueueItems()
        {
            bool allQueuesEmpty = true;


            Parallel.ForEach(_queues, (queue) =>
            {
                if (queue.Value.IsEmpty)
                {
                    return;
                }
                else
                {
                    allQueuesEmpty = false;
                }

                SharpDbTransaction sharpDbTransaction;

                bool succeeded = queue.Value.TryDequeue(out sharpDbTransaction);

                if (succeeded)
                {
                    if (sharpDbTransaction is WriteTransaction)
                    {
                        WriteTransaction writeTxn = (WriteTransaction)sharpDbTransaction;

                        _writer.WriteRow(writeTxn.Data, writeTxn.TableDefinition, writeTxn.AddressToWriteTo);

                        DataStore[sharpDbTransaction.Key] = new InsertResult { Successful = true };
                    }
                    else if (sharpDbTransaction is ReadTransaction)
                    {
                        ReadTransaction readTransaction = (ReadTransaction)sharpDbTransaction;

                        var rows = _reader.GetRows(sharpDbTransaction.TableDefinition, readTransaction.Selects, readTransaction.PredicateOperations);

                        DataStore[sharpDbTransaction.Key] = rows;
                    }
                }
                else
                {
                    //need to handle when dequeue does not succeed
                }
            });

            if(!allQueuesEmpty)
            {
                ProcessNextQueueItems();
            }
                

        }


    }
}
