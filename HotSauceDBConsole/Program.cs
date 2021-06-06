using HotSauceDb;
using HotSauceDb.Services;
using HotSauceDb.Services.Parsers;
using System;
using System.Collections.Generic;
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
                FullIntegration();

            }
            catch (Exception ex)
            {
                var x = Globals.GLOBAL_DEBUG;
            }
        }

        private static void AlterTableIntegration()
        {
            //group by currently only supported with plain sql
            var reader = new Reader();
            var writer = new Writer(reader);
            var schemaFetcher = new SchemaFetcher(reader);

            var interpreter = new Interpreter(
                new SelectParser(),
                new InsertParser(schemaFetcher),
                schemaFetcher,
                new GeneralParser(),
                new CreateParser(),
                new LockManager(writer, reader),
                reader);


            File.WriteAllText(Globals.FILE_NAME, null);


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

            File.WriteAllText("HotSauceDb.hdb", null);

            var reader = new Reader();
            var writer = new Writer(reader);
            var schemaFetcher = new SchemaFetcher(reader);

            var interpreter = new Interpreter(
                new SelectParser(),
                new InsertParser(schemaFetcher),
                schemaFetcher,
                new GeneralParser(),
                new CreateParser(),
                new LockManager(writer, reader),
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
            File.WriteAllText(Globals.FILE_NAME, null);

            //group by currently only supported with plain sql
            var reader = new Reader();
            var writer = new Writer(reader);
            var schemaFetcher = new SchemaFetcher(reader);

            var interpreter = new Interpreter(
                new SelectParser(),
                new InsertParser(schemaFetcher),
                schemaFetcher,
                new GeneralParser(),
                new CreateParser(),
                new LockManager(writer, reader),
                reader);


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

            AlterTableIntegration();

            //******

            if (!rowCountCorrect || !columnCountCorrect || !rowCountCorrect2 || !columnCountCorrect2 || !resultCountCorrect2 || !resultCountCorrect 
                || !toolSubQueryCompare || !compare || !groupedCountCorrect || !groupedValuesCorrect
                || !colCountCorrect || !orderIsCorrect || !insertCountCorrect || !updatedOneCorrect || !updatedTwoCorrect || !updatedRowsCountCorrect)
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
