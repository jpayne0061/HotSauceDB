using HotSauceDb;
using HotSauceDb.Services;
using HotSauceDb.Services.Parsers;
using HotSauceDbOrm;
using HotSauceIntegrationTests.TestModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HotSauceIntegrationTests
{
    [TestClass]
    public class IntegrationTests
    {
        public IntegrationTests()
        {
            Executor.GetInstance().DropDatabaseIfExists(); 
        }

        [TestMethod]
        public void JoinTests()
        {
            Executor executor = Executor.GetInstance();

            executor.CreateTable<Book>();
            executor.CreateTable<Author>();

            executor.Insert(new Author
            {
                Name = "Steinbeck",
                BirthDate = new DateTime(1920, 8, 8)
            });

            executor.Insert(new Author
            {
                Name = "Marquez",
                BirthDate = new DateTime(1915, 4, 26)
            });

            executor.Insert(new Book
            {
                Name = "One Hundred Years of Solitude",
                IsPublicDomain = false,
                NumberOfPages = 150,
                Price = 15.99m,
                ReleaseDate = new DateTime(1967, 1, 1),
                AuthorId = 2
            });

            executor.Insert(new Book
            {
                Name = "Slaghterhouse Five",
                IsPublicDomain = false,
                NumberOfPages = 150,
                Price = 15.99m,
                ReleaseDate = new DateTime(1969, 3, 31),
                AuthorId = 0
            });

            executor.Insert(new Book
            {
                Name = "Winter of Our Discontent",
                IsPublicDomain = false,
                NumberOfPages = 180,
                Price = 8.99m,
                ReleaseDate = new DateTime(1974, 12, 8),
                AuthorId = 1
            });

            executor.Insert(new Book
            {
                Name = "East of Eden",
                IsPublicDomain = false,
                NumberOfPages = 415,
                Price = 98.99m,
                ReleaseDate = new DateTime(1956, 12, 8),
                AuthorId = 1
            });


            List<Author> authors = executor.Read<Author>("select * from Author").Include<Author, Book>();

            Assert.AreEqual(authors[0].Name, "Steinbeck".PadRight(49));
            Assert.AreEqual(authors[1].Name, "Marquez".PadRight(49));
            Assert.AreEqual(authors[0].Books[0].Name, "Winter of Our Discontent".PadRight(49));
            Assert.AreEqual(authors[1].Books[0].Name, "One Hundred Years of Solitude".PadRight(49));
        }

        [TestMethod]
        public void Expressions_Tests()
        {
            Executor executor = Executor.GetInstance();

            executor.CreateTable<Person>();

            executor.Insert(new Person { Name = "Anna", Age = 11, Height = 56 });
            executor.Insert(new Person { Name = "Bob", Age = 62, Height = 59 });
            executor.Insert(new Person { Name = "Taylor", Age = 33, Height = 62 });

            var rows = executor.Read<Person>("select * from Person where Name = 'Anna' OR Name = 'Bob'");

            Assert.AreEqual(rows.Count, 2);

            rows = executor.Read<Person>("select * from Person where Name = 'Anna' OR Name = 'Bob' AND Age > 12 OR Age = 11");

            Assert.AreEqual(2, rows.Count);
        }

        [TestMethod]
        public void Alter_Table_Tests()
        {
            Executor executor = Executor.GetInstance();

            executor.CreateTable<Coffee>();

            executor.Insert(new Coffee { Name = "Haze", Price = 12.54m, SellByDate = new DateTime(2021, 12, 1) });
            executor.Insert(new Coffee { Name = "Pump", Price = 10.54m, SellByDate = new DateTime(2021, 11, 6) });
            executor.Insert(new Coffee { Name = "Hous", Price = 8.00m, SellByDate = new DateTime(2021, 8, 5) });

            var rows = executor.Read<Coffee>("select * from Coffee");

            Assert.AreEqual(3, rows.Count);

            executor.CreateTable<TestNamespace.Coffee>();

            executor.Insert(new TestNamespace.Coffee { Name = "Haze", Price = 12.54m, SellByDate = new DateTime(2021, 12, 1), Letter = 'C' });
            executor.Insert(new TestNamespace.Coffee { Name = "Pump", Price = 10.54m, SellByDate = new DateTime(2021, 11, 6), Letter = 'D' });
            executor.Insert(new TestNamespace.Coffee { Name = "Hous", Price = 8.00m, SellByDate = new DateTime(2021, 8, 5), Letter = ' ' });

            var rowAfterAlter = executor.Read<TestNamespace.Coffee>("select * from Coffee");

            Assert.AreEqual(rowAfterAlter.Count, 6);
            Assert.AreEqual(rowAfterAlter[5].Letter, ' ');
        }

        [TestMethod]
        public void ORMTests()
        {
            Executor executor = Executor.GetInstance();

            executor.CreateTable<Person>();

            executor.Insert(new Person { Name = "Anna", Age = 11, Height = 56 });

            var rows = executor.Read<Person>("select * from Person where Name = 'Anna'");

            Assert.AreEqual(1, rows.Count);
        }

        [TestMethod]
        public void InsertSpeedTest()
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

            Assert.AreEqual(count, houses.Count);

            Console.WriteLine($"Insert speed test results. Wrote {count} records in {seconds} seconds");
        }

        [TestMethod]
        public void UpdateORMTests()
        {
            Executor executor = Executor.GetInstance();

            decimal initialPrice = 430000;

            House h = new House();

            h.Address = "234 One St";
            h.IsListed = true;
            h.NumBath = 3;
            h.NumBedrooms = 5;
            h.Price = initialPrice;

            executor.CreateTable<House>();

            executor.Insert(h);

            h.Price = 500000;

            executor.Update(h);

            var h2 = executor.Read<House>("select * from house where houseid = 1");

            Assert.AreEqual(500000, h2.First().Price);
        }

        
        [TestMethod]
        public void ParallelTest()
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

            Assert.AreEqual(true, allHousesCountCorrect);

            Assert.AreEqual(true, insertCountCorrect);
        }



        [TestMethod]
        public void FullIntegration()
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
                string insertStatement = @"insert into houses values ('" + CreateString(10) + "'," +
                                           rd.Next().ToString() + "," + rd.Next().ToString() + "," + "true," + rd.Next().ToString() + ")";

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

            Assert.AreEqual(true, rowCountCorrect);
            Assert.AreEqual(true, columnCountCorrect);
            Assert.AreEqual(true, rowCountCorrect2);
            Assert.AreEqual(true, columnCountCorrect2);
            Assert.AreEqual(true, resultCountCorrect2);
            Assert.AreEqual(true, resultCountCorrect);
            Assert.AreEqual(true, toolSubQueryCompare);
            Assert.AreEqual(true, compare);
            Assert.AreEqual(true, groupedCountCorrect);
            Assert.AreEqual(true, groupedValuesCorrect);
            Assert.AreEqual(true, colCountCorrect);
            Assert.AreEqual(true, orderIsCorrect);
            Assert.AreEqual(true, insertCountCorrect);
            Assert.AreEqual(true, updatedOneCorrect);
            Assert.AreEqual(true, updatedTwoCorrect);
            Assert.AreEqual(true, updatedRowsCountCorrect);
            Assert.AreEqual(true, housesWithDateTimeCount);
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
    }
}
