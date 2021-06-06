using HotSauceDb.Services;
using HotSauceDbOrm.Operations;
using System;
using System.Collections.Generic;
using System.Text;

namespace HotSauceDbOrm.Operations
{
    public class Update : Insert
    {
        public Update(Interpreter interpreter) : base(interpreter)
        {

        }

        public void UpdateRecord<T>(T model)
        {

        }
    }
}
