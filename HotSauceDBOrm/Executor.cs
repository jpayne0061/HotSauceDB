using HotSauceDb;
using HotSauceDb.Services;
using HotSauceDb.Services.Parsers;
using HotSauceDB.Services;
using HotSauceDbOrm.Operations;
using System.Collections.Generic;
using System.IO;

namespace HotSauceDbOrm
{
    public class Executor
    {
        private Interpreter _interpreter;
        private Create      _creator;
        private Insert      _inserter;
        private Read        _reader;
        private Update      _updater;

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

                    var updateParser   = new UpdateParser();
                    var stringParser   = new StringParser();
                    var reader         = new Reader();
                    var writer         = new Writer(reader);
                    var lockManager    = new LockManager(writer, reader);
                    var schemaFetcher  = new SchemaFetcher(reader);
                    var selectParser   = new SelectParser();
                    var insertParser   = new InsertParser(schemaFetcher);
                    var generalParser  = new GeneralParser();
                    var createParser   = new CreateParser();
                    

                    var interpreter = new Interpreter(
                                        selectParser,
                                        insertParser,
                                        updateParser,
                                        schemaFetcher,
                                        generalParser,
                                        createParser,
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
            _inserter = new Insert(interpreter);
            _reader   = new Read  (interpreter);
            _creator  = new Create(interpreter, 
                                    new SchemaComparer(interpreter), 
                                    new DataMigrator(interpreter));

            _updater  = new Update(interpreter);

            _interpreter = interpreter;
        }

        public void CreateTable<T>() where T : class, new()
        {
            _creator.CreateTable<T>();
        }

        public void Insert<T>(T model) where T : class
        {
            _inserter.InsertRow(model);
        }

        public List<T> Read<T>(string query) where T : new()
        {
            return _reader.ReadRows<T>(query);
        }

        public void Update<T>(T model) where T : class
        {
            _updater.UpdateRecord<T>(model);
        }

        public void DropDatabaseIfExists()
        {
            _instance = null;
            File.WriteAllText(Constants.FILE_NAME, null);
        }

        public object ProcessRawQuery(string query)
        {
            return _interpreter.ProcessStatement(query);
        }
    }
}
