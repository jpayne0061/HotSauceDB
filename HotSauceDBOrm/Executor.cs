using HotSauceDb;
using HotSauceDb.Services;
using HotSauceDb.Services.Parsers;
using HotSauceDbOrm.Operations;
using System;
using System.Collections.Generic;
using System.IO;

namespace HotSauceDbOrm
{
    public class Executor
    {
        private Interpreter _interpreter;
        public Executor(string databaseName = Globals.FILE_NAME)
        {
            if(databaseName != null)
                if(!File.Exists(databaseName))
                    using (File.Create(databaseName));
                   

            var reader = new Reader();
            var writer = new Writer(reader);
            var lockManager = new LockManager(writer, reader);

            var schemaFetcher = new SchemaFetcher(reader);

            _interpreter = new Interpreter(
                                new SelectParser(),
                                new InsertParser(schemaFetcher),
                                schemaFetcher,
                                new GeneralParser(),
                                new CreateParser(),
                                lockManager,
                                reader);

            Creator = new Create(_interpreter);
            Inserter = new Insert(_interpreter);
            Reader = new Read(_interpreter);
            Updater = new Update(_interpreter);
        }

        private Create Creator { get; }
        private Insert Inserter { get; }
        private Read Reader { get; }
        private Update Updater { get; }


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

        public List<T> Update<T>(T model)
        {
            //need address of row to implement
            throw new NotImplementedException();
        }
        /// <summary>
        /// Processes any sql statement supported by HotSauceDb
        /// </summary>
        /// <returns>object</returns>
        public object ProcessRawQuery(string query)
        {
            return _interpreter.ProcessStatement(query);
        }
    }
}
