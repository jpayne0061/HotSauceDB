using HotSauceDb.Services;
using System;

namespace HotSauceDbOrm.Operations
{
    public class Insert : OperationsBase
    {
        protected Interpreter _interpreter;

        public Insert(Interpreter interepreter)
        {
            _interpreter = interepreter;
        }

        public void InsertRow<T>(T model)
        {
            IComparable[] row = GetRow(model);

            string tableName = model.GetType().Name;

            _interpreter.RunInsert(row, tableName);
        }
    }
}
