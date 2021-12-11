using HotSauceDb;
using HotSauceDb.Services;
using HotSauceDb.Services.Parsers;
using HotSauceDBIntegrationTests.TestModels;
using HotSauceDbOrm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HotSauceDbConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                //File.WriteAllText("HotSauceDb.hdb", null);
                Alter_Table_Tests();
                //Expressions_Tests();
                //InsertSpeedTest();
                //ORMTests();
                //UpdateORMTests();
                //FullIntegration();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Task failed successfully (not really): {ex.Message}. Stack trace: \n\n {ex.StackTrace}");
            }
        }

        private static void Expressions_Tests()
        {
            Executor executor = Executor.GetInstance();
            executor.CreateTable<Person>();

            executor.Insert(new Person { Name = "Anna", Age = 11, Height = 56 });
            executor.Insert(new Person { Name = "Bob", Age = 62, Height = 59 });
            executor.Insert(new Person { Name = "Taylor", Age = 33, Height = 62 });

            var rows = executor.Read<Person>("select * from Person where Name = 'Anna' OR Name = 'Bob'");

            if (rows.Count != 2)
            {
                throw new Exception("ORM Tests: count does not match");
            }

            rows = executor.Read<Person>("select * from Person where Name = 'Anna' OR Name = 'Bob' AND Age > 12 OR Age = 11");

            if (rows.Count != 2)
            {
                throw new Exception("ORM Tests: count does not match");
            }
        }

        private static void Alter_Table_Tests()
        {
            Executor executor = Executor.GetInstance();
            executor.CreateTable<Coffee>();

            //executor.Insert(new Coffee { Name = "Haze", Price = 12.54m, Ounces = 16, SellByDate = new DateTime(2021, 12, 1) });
            //executor.Insert(new Coffee { Name = "Pump", Price = 10.54m, Ounces = 18, SellByDate = new DateTime(2021, 11, 6) });
            //executor.Insert(new Coffee { Name = "Hous", Price = 8.00m, Ounces = 12, SellByDate = new DateTime(2021, 8, 5) });

            var rows = executor.Read<Coffee>("select * from Coffee");
        }

        private static void ORMTests()
        {
            Executor executor = Executor.GetInstance();

            executor.CreateTable<Person>();

            executor.Insert(new Person { Name = "Anna", Age = 11, Height = 56 });

            var rows = executor.Read<Person>("select * from Person where Name = 'Anna'");

            if(rows.Count != 1)
            {
                throw new Exception("ORM Tests: count does not match");
            }
        }

        private static void InsertSpeedTest()
        {
            Executor executor = Executor.GetInstance();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            var count = 100;

            for (int i = 0; i < count; i++)
            {
                House h = new House();

                h.Address = "234 One St";
                h.IsListed = true;
                h.NumBath = 3;
                h.NumBedrooms = 5;
                h.Price = 430000;

                executor.CreateTable<House>();

                executor.Insert(h);
            }

            sw.Stop();

            long seconds = sw.ElapsedMilliseconds / 1000;

            var houses = executor.Read<House>("Select * from house");

            if(houses.Count != count)
            {
                throw new Exception("count inserted doesn't match read");
            }

            Console.WriteLine($"Insert speed test results. Wrote {count} records in {seconds} seconds");
        }

        private static void UpdateORMTests()
        {
            decimal initialPrice = 430000;

            House h = new House();

            h.Address = "234 One St";
            h.IsListed = true;
            h.NumBath = 3;
            h.NumBedrooms = 5;
            h.Price = initialPrice;

            Executor executor = Executor.GetInstance();

            executor.CreateTable<House>();

            executor.Insert(h);

            h.Price = 500000;

            executor.Update(h);

            var h2 = executor.Read<House>("select * from house where houseid = 1");

            if(h2.First().Price != 500000)
            {

            }

            Console.WriteLine($"");
        }

        private static void AlterTableIntegration()
        {
            //group by currently only supported with plain sql
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


            File.WriteAllText(Constants.FILE_NAME, null);


            string createHousesTable = @"create table houses (
                                            Price decimal,
                                            Price2 decimal,
                                            Price3 decimal,
                                            Price4 decimal
                                       )";

            interpreter.ProcessStatement(createHousesTable);

            Random rd = new Random();

            for (int i = 0; i < 242; i++)
            {

                string insertStatement = @"insert into houses values (" + rd.Next().ToString() + "," +
                                           rd.Next().ToString() + "," + rd.Next().ToString() + "," + rd.Next().ToString() + ")";

                interpreter.ProcessStatement(insertStatement);
            }

            var housesOutBeforeAlter = (List<List<IComparable>>)interpreter.ProcessStatement("select * FROM houses");

            string alterTableDefinition = "Alter table houses add NumBathrooms int";

            interpreter.ProcessStatement(alterTableDefinition);

            var housesOut = (List<List<IComparable>>)interpreter.ProcessStatement("select * FROM houses");

            if(housesOut.Count != 242)
            {
                throw new Exception("row count doesnt match after alter table command");
            }

        }

        public static void ParallelTest()
        {
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


            interpreter.ProcessStatement(@"create table house4 (
                                                    NumBedrooms int,
                                                    NumBath int,
                                                    Price decimal,
                                                    IsListed bool,
                                                    Address varchar(50)
                                      )");


            var allHouses = new List<List<List<IComparable>>>();

            interpreter.ProcessStatement("insert into house4 values (5,3,295000,true,'800 Wormwood Dr')");

            Parallel.For(0, 200, i =>
            {
                interpreter.ProcessStatement("insert into house4 values (5,3,295000,true,'800 Wormwood Dr')");

                var houses = (List<List<IComparable>>)interpreter.ProcessStatement("select * FROM house4");

                allHouses.Add(houses);
            });

            var housesOut = (List<List<IComparable>>)interpreter.ProcessStatement("select * FROM house4");

            allHouses.Add(housesOut);

            var allHousesCountCorrect = allHouses.Count() == 201;

            var insertCountCorrect = allHouses[200].Count() == 201;


            if(!allHousesCountCorrect || !insertCountCorrect)
            {
                throw new Exception("err");
            }

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


        static void FullIntegration()
        {
            File.WriteAllText(Constants.FILE_NAME, null);

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

            string identityTable = @"create table Skateboards (
                                            SkateBoardId int Identity,
                                            Name varchar(100),
                                            Price decimal
                                       )";

            Random rd = new Random();

            interpreter.ProcessStatement(identityTable);

            var emptySkateboardRows = (List<List<IComparable>>)interpreter.ProcessStatement("select * from skateboards where Name = 'bob'");

            for (int i = 0; i < 500; i++)
            {
                string insertIdentity = "insert into Skateboards values ('HotSauce', " + rd.Next() + ")";

                interpreter.ProcessStatement(insertIdentity);
            }



            string readAllSkateboards = @"select * from Skateboards";


            var skateboardRows = (List<List<IComparable>>)interpreter.ProcessStatement(readAllSkateboards);

            string readSkateboardByName = @"select * from Skateboards where name = 'HotSauce'";


            var readSkateboardByNameRows = (List<List<IComparable>>)interpreter.ProcessStatement(readSkateboardByName);


            string createHousesTable = @"create table houses (
                                            Address varchar(100),
                                            Price decimal,
                                            SqFeet bigint,
                                            IsListed bool,
                                            NumBedrooms int,
                                       )";

            interpreter.ProcessStatement(createHousesTable);

            

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

            string selectInOperator = "select * from tools where name in (select name from tools)";

            var selectInOperatorRows = (List<List<IComparable>>)interpreter.ProcessStatement(selectInOperator);

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


            var toolSubQueryCompare = ((string)toolsSubQueryResult[0][0]).Trim() == "drill" && (decimal)toolsSubQueryResult[0][1] 
                == 45.99m && (bool)toolsSubQueryResult[0][3] == false;


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


            //parallel tests
            interpreter.ProcessStatement(@"create table house4 (
                                                    NumBedrooms int,
                                                    NumBath int,
                                                    Price decimal,
                                                    IsListed bool,
                                                    Address varchar(50)
                                      )");


            var allHouses = new List<List<List<IComparable>>>();

            interpreter.ProcessStatement("insert into house4 values (5,3,295000,true,'800 Wormwood Dr')");

            Parallel.For(0, 200, i =>
            {
                interpreter.ProcessStatement("insert into house4 values (5,3,295000,true,'800 Wormwood Dr')");

                var houses = (List<List<IComparable>>)interpreter.ProcessStatement("select * FROM house4");

                allHouses.Add(houses);
            });

            var housesOut = (List<List<IComparable>>)interpreter.ProcessStatement("select * FROM house4");

            allHouses.Add(housesOut);

            var allHousesCountCorrect = allHouses.Count() == 201;

            var insertCountCorrect = allHouses[200].Count() == 201;

            //UPDATE TESTS

            string createTable9 = @"create table houses9( Price int, NumBedRooms int, NumBathrooms int )";

            interpreter.ProcessStatement(createTable9);

            string insert19 = "insert into houses9 values (300000, 3, 2)";

            interpreter.ProcessStatement(insert19);

            string insert29 = "insert into houses9 values (310000, 4, 3)";

            interpreter.ProcessStatement(insert29);

            string insert39 = "insert into houses9 values (300000, 5, 4)";

            interpreter.ProcessStatement(insert39);

            string insert49 = "insert into houses9 values (330000, 6, 5)";

            interpreter.ProcessStatement(insert49);

            string insert59 = "insert into houses9 values (330000, 7, 6)";

            interpreter.ProcessStatement(insert59);

            string updateStatement = @"update houses9 Set Price = 440000, NumBathrooms = 90 where Numbedrooms = 7";

            interpreter.ProcessStatement(updateStatement);

            var updatedRows = (List<List<IComparable>>)interpreter.ProcessStatement("select * from houses9");

            bool updatedOneCorrect = (int)updatedRows[4][0] == 440000;

            bool updatedTwoCorrect = (int)updatedRows[4][2] == 90;

            bool updatedRowsCountCorrect = updatedRows.Count() == 5;

            string createTable10 = @"create table houses10( Price int, NumBedRooms int, NumBathrooms int, DateListed DateTime)";

            interpreter.ProcessStatement(createTable10);

            string insert60 = "insert into houses10 values (300000, 5, 4, '10/15/2021 9:03:37 pm')";

            interpreter.ProcessStatement(insert60);

            var housesWithDateTime = (List<List<IComparable>>)interpreter.ProcessStatement("select * from houses10 where DateListed > '10/15/2021 9:00:00 pm' ");

            bool housesWithDateTimeCount = housesWithDateTime.Count == 1;

            if (!rowCountCorrect || !columnCountCorrect || !rowCountCorrect2 || !columnCountCorrect2 || !resultCountCorrect2 || !resultCountCorrect 
                || !toolSubQueryCompare || !compare || !groupedCountCorrect || !groupedValuesCorrect
                || !colCountCorrect || !orderIsCorrect || !insertCountCorrect || !updatedOneCorrect || !updatedTwoCorrect || !updatedRowsCountCorrect || !housesWithDateTimeCount)
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
