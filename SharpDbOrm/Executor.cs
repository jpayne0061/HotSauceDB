using SharpDb.Services;
using SharpDb.Services.Parsers;
using SharpDbOrm.Operations;
using System;
using System.Collections.Generic;

namespace SharpDbOrm
{
    public class Executor
    {
        public Executor()
        {
            var reader = new Reader();
            var writer = new Writer();
            var lockManager = new LockManager(writer, reader);

            var schemaFetcher = new SchemaFetcher();

            var interpreter = new Interpreter(
                                new SelectParser(),
                                new InsertParser(schemaFetcher),
                                new SchemaFetcher(),
                                new GeneralParser(),
                                new CreateParser(),
                                lockManager,
                                reader);

            Creater = new Create(interpreter);
            Inserter = new Insert(interpreter);
            Reader = new Read(interpreter);
        }

        private Create Creater { get; }
        private Insert Inserter { get; }
        private Read Reader { get; }

        public void CreateTable<T>()
        {
            Creater.CreateTable<T>();
        }

        public void Insert<T>(T model)
        {
            Inserter.InsertRow(model);
        }

        public List<T> Read<T>(string query) where T : new()
        {
            return Reader.ReadRows<T>(query);
        }

    }
}
