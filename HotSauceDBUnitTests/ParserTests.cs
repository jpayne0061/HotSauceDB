using Microsoft.VisualStudio.TestTools.UnitTesting;
using HotSauceDb.Enums;
using HotSauceDb.Models;
using HotSauceDb.Services;
using HotSauceDb.Services.Parsers;
using System.Collections.Generic;
using System.Linq;
using Moq;
using HotSauceDB.Interfaces;

namespace HotSauceDbUnitTests
{
    [TestClass]
    public class ParserTests
    {
        Mock<ISchemaFetcher> _mockSchemaFetcher;

        [TestInitialize]
        public void ParserTestsInit()
        {
            _mockSchemaFetcher = new Mock<ISchemaFetcher>();
        }

        [TestMethod]
        public void GetColumns_Happy()
        {
            //arrange
            string query = "select name, origin, space, truck from someTable";

            SelectParser selectParser = new SelectParser();

            //act
            IList<string> columns = selectParser.GetColumns(query).Select(x => x.ColumnName).ToList();

            //assert
            Assert.AreEqual(columns[0], "name");
            Assert.AreEqual(columns[1], "origin");
            Assert.AreEqual(columns[2], "space");
            Assert.AreEqual(columns[3], "truck");
        }

        [TestMethod]
        public void GetTableName_Happy()
        {
            //arrange
            string query = "select name, origin, space, truck from someTable where origin > 2";

            SelectParser selectParser = new SelectParser();

            //act
            string tableName = selectParser.GetTableName(query);

            //assert
            Assert.AreEqual(tableName, "sometable");
        }

        [TestMethod]
        public void QueryHasWhereClause()
        {
            //arrange
            string query = "select name, origin, space, truck from someTable where origin > 2";

            SelectParser selectParser = new SelectParser();

            //act
            int idx = selectParser.IndexOfWhereClause(query, "sometable");

            //assert
            Assert.AreEqual(7, idx);
        }

        [TestMethod]
        public void QueryHasWhereClause_No_False_Alarm()
        {
            //arrange
            string query = "select where, origin, space, where from someTable";

            SelectParser selectParser = new SelectParser();

            //act
            int idx = selectParser.IndexOfWhereClause(query, "someTable");

            //assert
            Assert.AreEqual(-1, idx);
        }

        [TestMethod]
        public void ParsePredicates_Multiple_Predicates()
        {
            //arrange
            string query = @"select truck, origin, space
                            from someTable where origin > 8
                            AND truck = 'ford' 
                            OR space = 98";

            SelectParser selectParser = new SelectParser();

            //act
            List<string> predicates = selectParser.ParsePredicates(query).Predicates;

            //assert
            Assert.AreEqual("where origin > 8", predicates[0]);
            Assert.AreEqual("AND truck = 'ford'", predicates[1]);
            Assert.AreEqual("OR space = 98", predicates[2]);
        }

        [TestMethod]
        public void ParsePredicates_Multiple_Predicates_With_Spaces_In_Strings()
        {
            //arrange
            string query = @"select truck, origin, space
                            from someTable where origin > 8
                            AND truck = '   ford' 
                            OR space = 98";

            SelectParser selectParser = new SelectParser();

            //act
            List<string> predicates = selectParser.ParsePredicates(query).Predicates;

            //assert
            Assert.AreEqual("where origin > 8", predicates[0]);
            Assert.AreEqual("AND truck = '   ford'", predicates[1]);
            Assert.AreEqual("OR space = 98", predicates[2]);
        }

        [TestMethod]
        public void ParsePredicates_With_Trailing_Predicates_One_Column()
        {
            //arrange
            string query = @"select truck, origin, space
                            from someTable where origin > 8
                            AND truck = '   ford' 
                            OR space = 98
                            ORDER BY truck";

            SelectParser selectParser = new SelectParser();

            //act
            var predicateResults = selectParser.ParsePredicates(query);

            predicateResults = selectParser.GetPredicateTrailers(predicateResults, query);

            var predicates = predicateResults.Predicates;



            //assert
            Assert.AreEqual("where origin > 8", predicates[0]);
            Assert.AreEqual("AND truck = '   ford'", predicates[1]);
            Assert.AreEqual("OR space = 98", predicates[2]);
            Assert.AreEqual("order truck", predicateResults.PredicateTrailer[0]);
        }

        [TestMethod]
        public void ParsePredicates_With_Trailing_Predicates_Two_Columns()
        {
            //arrange
            string query = @"select truck, origin, space
                            from someTable where origin > 8
                            AND truck = '   ford' 
                            OR space = 98
                            ORDER BY truck, origin";

            SelectParser selectParser = new SelectParser();

            //act
            var predicateResults = selectParser.ParsePredicates(query);

            var predicates = predicateResults.Predicates;



            //assert
            Assert.AreEqual("where origin > 8", predicates[0]);
            Assert.AreEqual("AND truck = '   ford'", predicates[1]);
            Assert.AreEqual("OR space = 98", predicates[2]);
        }

        [TestMethod]
        public void GetFirstMostInnerSelectStatement_Happy()
        {
            //arrange
            SelectParser selectParser = new SelectParser();

            string query = @"select truck, origin, space
                            from someTable where origin > 8
                            AND truck = (select truck from someTruckTable) 
                            OR space = 98";


            //act
            InnerStatement subquery = selectParser.GetFirstMostInnerParantheses(query);

            //assert
            Assert.AreEqual("select truck from someTruckTable", subquery.Statement);
            Assert.AreEqual(130, subquery.StartIndexOfOpenParantheses);
            Assert.AreEqual(163, subquery.EndIndexOfCloseParantheses);
        }


        [TestMethod]
        public void GetFirstMostInnerSelectStatement_Parses_With_Spaces()
        {
            //arrange
            SelectParser selectParser = new SelectParser();

            string query = @"select truck, origin, space
                            from someTable where origin > 8
                            AND truck = (   select truck from someTruckTable   ) 
                            OR space = 98";


            //act
            InnerStatement subquery = selectParser.GetFirstMostInnerParantheses(query);

            //assert
            Assert.AreEqual("   select truck from someTruckTable   ", subquery.Statement);
            Assert.AreEqual(130, subquery.StartIndexOfOpenParantheses);
            Assert.AreEqual(169, subquery.EndIndexOfCloseParantheses);
        }

        [TestMethod]
        public void ReplaceSubqueryWithValue()
        {
            //arrange
            SelectParser selectParser = new SelectParser();

            string query = @"select truck, origin, space
                            from someTable where origin > 8
                            AND truck = (   select truck from someTruckTable   ) 
                            OR space = 98";

            InnerStatement subquery = selectParser.GetFirstMostInnerParantheses(query);

            var expected = @"select truck, origin, space
                            from someTable where origin > 8
                            AND truck = 'F-150' 
                            OR space = 98";

            //act
            var newQuery = new SelectParser().ReplaceSubqueryWithValue(query, subquery, "F-150", TypeEnum.String);


            //assert
            Assert.AreEqual(expected, newQuery);
        }

        [TestMethod]
        public void ReplaceSubqueryWithValue_Handle_New_Lines()
        {
            //arrange
            SelectParser selectParser = new SelectParser();

            string query = @"select truck, origin, space
                            from someTable where origin > 8
                            AND truck = (  
                                            select truck from someTruckTable   
                                        ) 
                            OR space = 98";

            InnerStatement subquery = selectParser.GetFirstMostInnerParantheses(query);

            var expected = @"select truck, origin, space
                            from someTable where origin > 8
                            AND truck = 'F-150' 
                            OR space = 98";

            //act
            var newQuery = new SelectParser().ReplaceSubqueryWithValue(query, subquery, "F-150", TypeEnum.String);


            //assert
            Assert.AreEqual(expected, newQuery);
        }

        [TestMethod]
        public void InsertParser_ParseTableName()
        {
            //arrange
            var insertParser = new InsertParser(_mockSchemaFetcher.Object);

            string dml = "insert into myTable VALUES ('one', 'two', 'three')";
            string expected = "mytable";

            //act
            string tableName = insertParser.ParseTableName(dml);

            //assert
            Assert.AreEqual(expected, tableName.ToLower());
        }

        [TestMethod]
        public void GetOuterMostParantheses()
        {
            //arrange
            var genParser = new GeneralParser();

            string dml = @"create table houses(
                                Address varchar(100),
                                Price decimal
                            )";

            string expected = @"
                                Address varchar(100),
                                Price decimal
                            ";

            //act
            string statement = genParser.GetOuterMostParantheses(dml).Statement;

            //assert
            Assert.AreEqual(expected, statement);
        }

        [TestMethod]
        public void GetInnerMostSelectStatement()
        {
            //arrange
            string select = @"select * from somTable WHERE col IN (3,4,5) and colz = (select * from blag)";

            var parser = new SelectParser();

            //act

            string result = parser.GetInnerMostSelectStatement(select).Statement;

            Assert.AreEqual("select * from blag", result);
        }

        [TestMethod]
        public void GetInnerMostSelectStatement_WithNewLines()
        {
            //arrange
            string select = @"select * from somTable WHERE col IN (3,4,5) and colz = (
                                    select * from blag

)";


            var parser = new SelectParser();

            var expected = @"
                                    select * from blag

";

            //act

            string result = parser.GetInnerMostSelectStatement(select).Statement;

            Assert.AreEqual(expected, result);
        }


        [TestMethod]
        public void ParseTableNameUpdate()
        {

            UpdateParser updateParser = new UpdateParser();

            string tableName = updateParser.GetTableName("update loops where col1 = '345'");

            Assert.AreEqual("loops", tableName);
        }

        [TestMethod]
        public void GetSetClause()
        {

            UpdateParser updateParser = new UpdateParser();

            List<KeyValuePair<string, string>> setClause = updateParser.GetUpdates(@"update houses
                                                                                    Set Price = 456000, Address = 'gtggtt', Neigbs = 'gfff'
                                                                                    where houseID = 908");

            Assert.AreEqual(3, setClause.Count);
            Assert.AreEqual("price",    setClause[0].Key.ToLower());
            Assert.AreEqual("address",  setClause[1].Key.ToLower());
            Assert.AreEqual("neigbs",   setClause[2].Key.ToLower());
            Assert.AreEqual("456000",   setClause[0].Value.ToLower());
            Assert.AreEqual("'gtggtt'", setClause[1].Value.ToLower());
            Assert.AreEqual("'gfff'",   setClause[2].Value.ToLower());
        }

    }
}


