using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDb.Services.Parsers;
using System.Collections;
using System.Collections.Generic;

namespace SharpDbUnitTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void GetColumns_Happy()
        {
            //arrange
            string query = "select name, origin, space, truck from someTable";

            SelectParser selectParser = new SelectParser();

            //act
            IList<string> columns = selectParser.GetColumns(query);

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
            Assert.AreEqual(tableName, "someTable");
        }

        [TestMethod]
        public void QueryHasWhereClause()
        {
            //arrange
            string query = "select name, origin, space, truck from someTable where origin > 2";

            SelectParser selectParser = new SelectParser();

            //act
            int idx = selectParser.IndexOfWhereClause(query, "someTable");

            //assert
            Assert.AreEqual(-7, idx);
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
        public void ParsePredicates_One_Predicate()
        {
            //arrange
            string query = "select where, origin, space, where from someTable where origin > 8";

            SelectParser selectParser = new SelectParser();

            //act
            List<string> predicates = selectParser.ParsePredicates(query);

            //assert
            Assert.AreEqual("where origin > 8", predicates[0]);
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
            List<string> predicates = selectParser.ParsePredicates(query);

            //assert
            Assert.AreEqual("where origin > 8", predicates[0]);
            Assert.AreEqual("and truck = 'ford'", predicates[1]);
            Assert.AreEqual("or space = 98", predicates[2]);
        }

    }
}
