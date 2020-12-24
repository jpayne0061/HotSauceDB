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
            var writer = new Writer(reader);
            var lockManager = new QueryCoordinator(writer, reader);

            var schemaFetcher = new SchemaFetcher(reader);

            var interpreter = new Interpreter(
                                new SelectParser(),
                                new InsertParser(schemaFetcher),
                                schemaFetcher,
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
