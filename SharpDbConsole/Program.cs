using SharpDb;
using SharpDb.Enums;
using SharpDb.Models;
using SharpDb.Services;
using SharpDb.Services.Parsers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpDbConsole
{
    class Program
    {
        static void Main(string[] args)
        {

            try
            {
                FullIntegration();
                //TestGroupBy();
                //TestParsingGroupBy();

                //var reader = new Reader();
                //var writer = new Writer();

                //var lockManager = new LockManager(writer, reader);

                //var schemaFetcher = new SchemaFetcher();

                //var interpreter = new Interpreter(
                //                    new SelectParser(),
                //                    new InsertParser(schemaFetcher),
                //                    new SchemaFetcher(),
                //                    new GeneralParser(),
                //                    new CreateParser(),
                //                    lockManager,
                //                    reader);

                //string query = @"select * from houses order by price";

                //var rows = interpreter.ProcessStatement(query);

                //var x = 0;

                //
                //QueryWithOrderBy();
                //ConcurrentStack<char> stack = new ConcurrentStack<char>();

//                Globals.FILE_NAME = "integration.txt";

//                //                string query = "select * from houses";

//                string insertStatement7 = @"insert into houses values ('" + "999 Adams St" + "'," +
//"270000" + "," + "2300" + "," + "false," + "3" + ")";


//                List<List<List<IComparable>>> l = new List<List<List<IComparable>>>();

//                insertStatement7 = @"insert into houses values ('" + "777990 Adams St" + "'," +
//"270000" + "," + "2300" + "," + "false," + "3" + ")";


//                Parallel.For(0, 500,
//                         index =>
//                         {

//                             if (index % 2 == 0)
//                             {
//                                 l.Add((List<List<IComparable>>)interpreter.ProcessStatement("select * from houses"));
//                             }
//                             else
//                             {
//                                 interpreter.ProcessStatement(insertStatement7);
//                             }

//                         });



                //                string queryx = "select * from houses where address = '777990 Adams St'";

                //                var rows = (List<List<IComparable>>)interpreter.ProcessStatement(queryx);


                //                var x = 9;

                //
                //RunInStatement();
                //RunInStatement();
                //WriteTablesAndFillRows();

                //ProcessCreateTableStatement();


                //SelectWithPredicates();

                //InsertRows();

                //WriteTablesAndFillRows();

                //for (int i = 0; i < 20; i++)
                //{
                //    var t = BuildToolsTable();

                //    var p = BuildPersonTable();

                //    var writer = new Writer();

                //    writer.WriteTableDefinition(t);
                //    writer.WriteTableDefinition(p);


                //WriteTablesAndFillRows();

                //InsertRows();
                //SelectWithSubQueries();
                //SelectWithPredicates();
                //SelectWithSubQueries();
                //    var reader = new Reader();

                //    var indexPage = reader.GetIndexPage();

                //    var tableDef = indexPage.TableDefinitions.Where(x => x.TableName == "Tools").FirstOrDefault();

                //    var writer = new Writer();

                //    object[] rowz = new object[3];

                //    rowz[0] = "hammerTime";
                //    rowz[1] = 44.99m;
                //    rowz[2] = 29;

                //    writer.WriteRow(rowz, tableDef);

                //object[] rowz3 = new object[3];

                //rowz3[0] = " A very cool Drill ";
                //rowz3[1] = 56.99m;
                //rowz3[2] = 256;

                //writer.WriteRow(rowz3, tableDef);


                //object[] rowz2 = new object[3];

                //rowz2[0] = "A big hammer  ";
                //rowz2[1] = 678.99m;
                //rowz2[2] = 89;

                //writer.WriteRow(rowz2, tableDef);

                //var stream = File.Create("data.txt");

                //stream.Close();

                //FillRows();

                //SelectWithPredicates();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        static T ReturnInstance<T>(int x) where T : new()
        {
            return new T();
        }

        private static TableDefinition BuildToolsTable()
        {
            TableDefinition table = new TableDefinition();
            table.TableName = "Tools";

            table.ColumnDefinitions = new List<ColumnDefinition>();

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 0, Type = TypeEnum.String, ByteSize = 41, ColumnName = "ToolName" });

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 1, Type = TypeEnum.Decimal, ByteSize = Globals.DecimalByteLength, ColumnName = "Price" });

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 2, Type = TypeEnum.Int32, ByteSize = Globals.Int32ByteLength, ColumnName = "NumInStock" });

            return table;
        }

        private static TableDefinition BuildPersonTable()
        {
            TableDefinition table = new TableDefinition();
            table.TableName = "Person";

            table.ColumnDefinitions = new List<ColumnDefinition>();

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 0, Type = TypeEnum.String, ByteSize = 21, ColumnName = "Name" });

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 1, Type = TypeEnum.Int32, ByteSize = Globals.Int32ByteLength, ColumnName = "Age" });

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 2, Type = TypeEnum.Boolean, ByteSize = Globals.BooleanByteLength, ColumnName = "IsAdult" });

            return table;
        }

        private static TableDefinition BuildFamilyTable()
        {
            TableDefinition table = new TableDefinition();
            table.TableName = "Family";

            table.ColumnDefinitions = new List<ColumnDefinition>();

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 0, Type = TypeEnum.String, ByteSize = 21, ColumnName = "FamilyName" });

            table.ColumnDefinitions.Add(new ColumnDefinition { Index = 1, Type = TypeEnum.Int32, ByteSize = Globals.Int32ByteLength, ColumnName = "NumberMembers" });

            return table;
        }

        internal static string CreateString(int stringLength)
        {
            Random rd = new Random();

            const string allowedChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789";
            char[] chars = new char[stringLength];

            for (int i = 0; i < stringLength; i++)
            {
                chars[i] = allowedChars[rd.Next(0, allowedChars.Length)];
            }

            return new string(chars);
        }



        public static void SelectWithPredicates()
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
                                new LockManager(writer, reader),
                                reader);

            string query = "select ToolName, Price from tools where NumInStock = 4";

            //string query = "select ToolName, Price from Tools WHERE ToolName = 'A very cool Drill' AND Price > 250.00 OR NumInStock = 23";

            //string query = "select * from Tools WHERE price > 500";

            //string query = "select * from Person WHERE IsAdult = false";

            var rows = interpreter.RunQuery(query);
        }

        public static void SelectWithSubQueries()
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
                                new LockManager(writer, reader),
                                reader);

            string query = @"select ToolName, Price
                               From tools where NumInStock = (
                                        select NumInStock FROM tools 
                                        where ToolName = 
                                                    (
                                                        Select * from tools where ToolName = 'nail puller 2'
                                                    )
                                        )";

            //string query = "select * from Tools WHERE price > 500";

            //string query = "select * from Person WHERE IsAdult = false";

            var rows = interpreter.ProcessStatement(query);
        }

        static void InsertRows()
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
                                new LockManager(writer, reader),
                                reader);

            var insertParser = new InsertParser(new SchemaFetcher());

            string dml = "insert into tools VALUES ('nail puller 2', 15.99, 34)";

            interpreter.ProcessStatement(dml);
        }

        static void ProcessCreateTableStatement()
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
                                new LockManager(writer, reader),
                                reader);

            string dml = @"create table Houses(
                                Address varchar(100),
                                Price decimal,
                                IsListed bool,
                                SquareFeet bigint,
                                NumBedRooms int
                            )";

            interpreter.ProcessStatement(dml);

            string insert = "insert into houses values ('123 abc street', 341000, true, 2300, 3)";

            interpreter.ProcessStatement(insert);

            string select = "select * from houses where address = '123 abc street' AND price = 341000";

            List<List<IComparable>> rows = (List<List<IComparable>>)interpreter.ProcessStatement(select);

            var x = 0;

        }

        static void RunInStatement()
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
                                new LockManager(writer, reader),
                                reader);

            //string insert = "insert into houses values ('123 abc street', 345000, true, 2300, 3)";

            //interpreter.ProcessStatement(insert);

            //string insert2 = "insert into houses values ('123 abc street', 360000, true, 2300, 3)";

            //interpreter.ProcessStatement(insert2);

            string select = @"select price, address from houses where price in (
                        select price from houses where address = '123 abc street'
)";

            var rows = interpreter.ProcessStatement(select);
        }

        static void RunInStatementWithSubqueries()
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
                                new LockManager(writer, reader),
                                reader);

            //string insert = "insert into houses values ('123 abc street', 345000, true, 2300, 3)";

            //interpreter.ProcessStatement(insert);

            //string insert2 = "insert into houses values ('4500 Cool street', 389000, true, 2300, 3)";

            //interpreter.ProcessStatement(insert2);

            //string insert3 = "insert into houses values ('4500 Cool street', 389000, true, 2300, 5)";

            //interpreter.ProcessStatement(insert3);

            string select = @"select * from houses where price in (341000, 365000)
                    and Address = (select address from houses where price = 389000)
                        or NumBedrooms = 5";
             


            var rows = interpreter.ProcessStatement(select);
        }

        static void QueryWithOrderBy()
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
                                new LockManager(writer, reader),
                                reader);

            string select = @"select * from houses where price > 200000 order by price, address";

            var rows = interpreter.ProcessStatement(select);
        }

        static void TestGroupBy()
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
                                new LockManager(writer, reader),
                                reader);

            string createTable = @"create table houses( Price int, NumBedRooms int, NumBathrooms int )";

            interpreter.ProcessStatement(createTable);

            string insert1 = "insert into houses values (300000, 3, 2)";

            interpreter.ProcessStatement(insert1);

            string insert2 = "insert into houses values (300000, 4, 3)";

            interpreter.ProcessStatement(insert2);

            string insert3 = "insert into houses values (300000, 5, 4)";

            interpreter.ProcessStatement(insert3);

            string insert4 = "insert into houses values (330000, 6, 5)";

            interpreter.ProcessStatement(insert4);

            string insert5 = "insert into houses values (330000, 7, 6)";

            interpreter.ProcessStatement(insert5);



            string select = @"select Price, Max(NumBedRooms), Min(NumBathrooms)
                             from houses
                                GROUP BY PRICE";

            var rows = interpreter.ProcessStatement(select);
        }

        static void FullIntegration()
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
                                new LockManager(writer, reader),
                                reader);

            //if integration.txt exists, delete and recreate
            File.WriteAllText("integration.txt", null);

            Globals.FILE_NAME = "integration.txt";


            string createHousesTable = @"create table houses (
                                            Address varchar(100),
                                            Price decimal,
                                            SqFeet bigint,
                                            IsListed bool,
                                            NumBedrooms int
                                       )";

            interpreter.ProcessStatement(createHousesTable);

            Random rd = new Random();

            for (int i = 0; i < 200; i++)
            {

                string insertStatement = @"insert into houses values ('" + CreateString(10) + "',"+
                                           rd.Next().ToString() +"," + rd.Next().ToString() + "," + "true," + rd.Next().ToString() + ")";

                interpreter.ProcessStatement(insertStatement);
            }

            string insertStatement2 = @"insert into houses values ('" + "450 Adams St" + "'," +
                           "320000" + "," + "2300" + "," + "false," + "3" + ")";

            interpreter.ProcessStatement(insertStatement2);


            string createToolsTable = @"create table tools (
                                            Name varchar(30),
                                            Price decimal,
                                            NumInStock bigint,
                                            IsWooden bool,
                                            Manufacturer varchar(50)
                                       )";

            interpreter.ProcessStatement(createToolsTable);


            for (int i = 0; i < 500; i++)
            {

                string insertStatement = @"insert into tools values ('" + CreateString(10) + "'," +
                                           rd.Next().ToString() + "," + rd.Next().ToString() + "," + "true," + CreateString(10) + ")";

                interpreter.ProcessStatement(insertStatement);
            }


            string insertStatement3 = @"insert into tools values ('" + "hammer" + "'," +
                                           "23.99" + "," + "67" + "," + "false," + "'craftsman'" + ")";

            interpreter.ProcessStatement(insertStatement3);

            for (int i = 0; i < 200; i++)
            {

                string insertStatement = @"insert into houses values ('" + CreateString(10) + "'," +
                                           rd.Next().ToString() + "," + rd.Next().ToString() + "," + "true," + rd.Next().ToString() + ")";

                interpreter.ProcessStatement(insertStatement);
            }


            string insertStatement6 = @"insert into houses values ('" + "999 Adams St" + "'," +
"269000" + "," + "2300" + "," + "false," + "3" + ")";

            interpreter.ProcessStatement(insertStatement6);

            string insertStatement7 = @"insert into houses values ('" + "999 Adams St" + "'," +
"270000" + "," + "2300" + "," + "false," + "3" + ")";

            interpreter.ProcessStatement(insertStatement7);

            string insertStatement4 = @"insert into tools values ('" + "drill" + "'," +
                               "45.99" + "," + "90" + "," + "false," + "'dewalt'" + ")";

            interpreter.ProcessStatement(insertStatement4);


            for (int i = 0; i < 250; i++)
            {

                string insertStatement = @"insert into tools values ('" + CreateString(10) + "'," +
                                           rd.Next().ToString() + "," + rd.Next().ToString() + "," + "true," + CreateString(10) + ")";

                interpreter.ProcessStatement(insertStatement);
            }

            //houses count should be: 1401

            //tools count should be: 752

            string readAllHouses = @"select * from houses";


            var rows = (List<List<IComparable>>)interpreter.ProcessStatement(readAllHouses);

            bool rowCountCorrect = rows.Count() == 403;
            bool columnCountCorrect = rows[0].Count() == 5;


            string readAllTools = @"select price, numInstock from tools";


            var tools = (List<List<IComparable>>)interpreter.ProcessStatement(readAllTools);

            bool rowCountCorrect2 = tools.Count() == 752;
            bool columnCountCorrect2 = tools[0].Count() == 2;


            string querySearchHousesByName = @"select * from houses where address = '450 Adams St'";


            var result = (List<List<IComparable>>)interpreter.ProcessStatement(querySearchHousesByName);

            bool resultCountCorrect = result.Count() == 1;

            string querySearchHousesByNameAndPrice = @"select * 
                                                    from houses 
                                                     where address = '450 Adams St'
                                                      AND price > 315000";


            var result2 = (List<List<IComparable>>)interpreter.ProcessStatement(querySearchHousesByNameAndPrice);

            bool resultCountCorrect2 = result2.Count() == 1;

            string subQueryTools = @"select * from tools 
                                        where name = (select name from tools where price = 45.99 )";

            var toolsSubQueryResult = (List<List<IComparable>>)interpreter.ProcessStatement(subQueryTools);

            //string insertStatement4 = @"insert into tools values ('" + "drill" + "'," +
            //       "45.99" + "," + "90" + "," + "false," + "'dewalt'" + ")";

            var toolSubQueryCompare = ((string)toolsSubQueryResult[0][0]).Trim() == "drill" && (decimal)toolsSubQueryResult[0][1] 
                == 45.99m && (bool)toolsSubQueryResult[0][3] == false;


            //string toolsInClause = @"select * from tools 
            //                            where name IN (select name from tools where price > 20 )";

            string toolsInClause = @"select * from tools 
                                        where name IN ('drill', 'hammer' )";

            var toolsInClauseResults = (List<List<IComparable>>)interpreter.ProcessStatement(toolsInClause);

            var compare = toolsInClauseResults.Count() == 2;


            string selectWithPredicatesAndOrderBy = @"select * from houses
                                                      where address != '98765 ABC str'
                                                       AND Price > 269000
                                                        order by price";

            var predicatesAndOrderResults = (List<List<IComparable>>)interpreter.ProcessStatement(selectWithPredicatesAndOrderBy);

            bool colCountCorrect = ((int)predicatesAndOrderResults[0].Count()) == 5;

            bool orderIsCorrect = ((decimal)predicatesAndOrderResults[1][1]) > ((decimal)predicatesAndOrderResults[0][1])
                                && ((decimal)predicatesAndOrderResults[15][1]) > ((decimal)predicatesAndOrderResults[6][1])
                                && ((decimal)predicatesAndOrderResults[90][1]) > ((decimal)predicatesAndOrderResults[89][1])
                                && ((decimal)predicatesAndOrderResults[100][1]) > ((decimal)predicatesAndOrderResults[98][1])
                                && ((decimal)predicatesAndOrderResults[120][1]) > ((decimal)predicatesAndOrderResults[118][1])
                                && ((decimal)predicatesAndOrderResults[150][1]) > ((decimal)predicatesAndOrderResults[145][1]);


            //*******group by tests

            string createTable = @"create table houses2( Price int, NumBedRooms int, NumBathrooms int )";

            interpreter.ProcessStatement(createTable);

            string insert1 = "insert into houses2 values (300000, 3, 2)";

            interpreter.ProcessStatement(insert1);

            string insert2 = "insert into houses2 values (300000, 4, 3)";

            interpreter.ProcessStatement(insert2);

            string insert3 = "insert into houses2 values (300000, 5, 4)";

            interpreter.ProcessStatement(insert3);

            string insert4 = "insert into houses2 values (330000, 6, 5)";

            interpreter.ProcessStatement(insert4);

            string insert5 = "insert into houses2 values (330000, 7, 6)";

            interpreter.ProcessStatement(insert5);



            string select = @"select Price, Max(NumBedRooms), Min(NumBathrooms)
                             from houses2
                                GROUP BY PRICE";

            var groupedRows = (List<List<IComparable>>)interpreter.ProcessStatement(select);

            var groupedCountCorrect = groupedRows.Count() == 2;

            var groupedValuesCorrect = (int)groupedRows[0][0] == 300000
                             && (int)groupedRows[0][1] == 5
                             && (int)groupedRows[0][2] == 2
                             && (int)groupedRows[1][0] == 330000
                             && (int)groupedRows[1][1] == 7
                             && (int)groupedRows[1][2] == 5;


            //******

            if (!rowCountCorrect || !columnCountCorrect || !rowCountCorrect2 || !columnCountCorrect2 || !resultCountCorrect2 || !resultCountCorrect 
                || !toolSubQueryCompare || !compare || !groupedCountCorrect || !groupedValuesCorrect
                || !colCountCorrect || !orderIsCorrect)
            {
                throw new Exception("tests failed");
            }
            else
            {
                Console.WriteLine("SUCCESS!!");
            }
        }

    }
}
