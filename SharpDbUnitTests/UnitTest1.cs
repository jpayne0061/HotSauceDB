using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDb.Enums;
using SharpDb.Models;
using SharpDb.Services;
using SharpDb.Services.Parsers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
            List<string> predicates = selectParser.ParsePredicates(query);

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
            Subquery subquery = selectParser.GetFirstMostInnerSelectStatement(query);

            //assert
            Assert.AreEqual("select truck from someTruckTable", subquery.Query);
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
            Subquery subquery = selectParser.GetFirstMostInnerSelectStatement(query);

            //assert
            Assert.AreEqual("   select truck from someTruckTable   ", subquery.Query);
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

            Subquery subquery = selectParser.GetFirstMostInnerSelectStatement(query);

            var interpreter = new Interpreter(selectParser);

            var expected = @"select truck, origin, space
                            from someTable where origin > 8
                            AND truck = 'F-150' 
                            OR space = 98";

            //act
            var newQuery = interpreter.ReplaceSubqueryWithValue(query, subquery, "F-150", TypeEnums.String);


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

            Subquery subquery = selectParser.GetFirstMostInnerSelectStatement(query);

            var interpreter = new Interpreter(selectParser);

            var expected = @"select truck, origin, space
                            from someTable where origin > 8
                            AND truck = 'F-150' 
                            OR space = 98";

            //act
            var newQuery = interpreter.ReplaceSubqueryWithValue(query, subquery, "F-150", TypeEnums.String);


            //assert
            Assert.AreEqual(expected, newQuery);
        }


    }
}
