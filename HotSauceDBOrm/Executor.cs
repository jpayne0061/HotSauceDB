using SharpDb;
using SharpDb.Services;
using SharpDb.Services.Parsers;
using SharpDbOrm.Operations;
using System;
using System.Collections.Generic;
using System.IO;

namespace SharpDbOrm
{
    public class Executor
    {
        public Executor(string databaseName = Globals.FILE_NAME)
        {
            if(databaseName != null)
                if(!File.Exists(databaseName))
                    using (File.Create(databaseName));
                   

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

            Creator = new Create(interpreter);
            Inserter = new Insert(interpreter);
            Reader = new Read(interpreter);
        }

        private Create Creator { get; }
        private Insert Inserter { get; }
        private Read Reader { get; }


        public void CreateTable<T>()
        {
            Creator.CreateTable<T>();
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
