using HotSauceDb;
using HotSauceDb.Services;
using HotSauceDb.Services.Parsers;
using HotSauceDbOrm.Operations;
using System.Collections.Generic;
using System.IO;

namespace HotSauceDbOrm
{
    public class Executor
    {
        private Interpreter _interpreter;
        private Create _creator;
        private Insert _inserter;
        private Read _reader;
        private Update _updater;

        private static Executor _instance;
        private static object _lockObject = new object();

        public static Executor GetInstance(string databaseName = Constants.FILE_NAME)
        {
            if (_instance == null)
            {
                lock(_lockObject)
                {
                    if (databaseName != null)
                        if (!File.Exists(databaseName))
                            using (File.Create(databaseName)) ;

                    var updateParser = new UpdateParser();
                    var stringParser = new StringParser();
                    var reader = new Reader();
                    var writer = new Writer(reader);
                    var lockManager = new LockManager(writer, reader);
                    var schemaFetcher = new SchemaFetcher(reader);

                    var interpreter = new Interpreter(
                                        new SelectParser(),
                                        new InsertParser(schemaFetcher),
                                        updateParser,
                                        schemaFetcher,
                                        new GeneralParser(),
                                        new CreateParser(),
                                        stringParser,
                                        lockManager,
                                        reader);

                    _instance = new Executor(interpreter);
                }
            }

            return _instance;
        }

        private Executor(Interpreter interpreter)
        {
            _creator = new Create(interpreter);
            _inserter = new Insert(interpreter);
            _reader = new Read(interpreter);
            _updater = new Update(interpreter);

            _interpreter = interpreter;
        }

        public void CreateTable<T>()
        {
            _creator.CreateTable<T>();
        }

        public void Insert<T>(T model)
        {
            _inserter.InsertRow(model);
        }

        public List<T> Read<T>(string query) where T : new()
        {
            return _reader.ReadRows<T>(query);
        }

        public void Update<T>(T model)
        {
            _updater.UpdateRecord<T>(model);
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
