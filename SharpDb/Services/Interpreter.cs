using SharpDb.Services.Parsers;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpDb.Services
{
    public class Interpreter
    {
        private SelectParser _selectParser;

        public Interpreter(SelectParser selectParser)
        {
            _selectParser = selectParser;
        }

        public List<List<IComparable>> RunQuery(string query)
        {
            var reader = new Reader();

            var tableName = _selectParser.GetTableName(query);

            var predicates = _selectParser.ParsePredicates(query);

            var predicateOperations = _selectParser.BuildDelagatesFromPredicates(tableName, predicates);

            return reader.GetRowsWithPredicate(tableName, predicateOperations);
        }

    }
}
