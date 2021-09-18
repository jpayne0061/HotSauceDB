﻿using HotSauceDb.Models;
using HotSauceDb.Services;
using System;
using System.Linq;
using System.Reflection;

namespace HotSauceDbOrm.Operations
{
    public class Insert : OperationsBase
    {
        protected Interpreter _interpreter;

        public Insert(Interpreter interepreter)
        {
            _interpreter = interepreter;
        }

        public void InsertRow<T>(T obj)
        {
            IComparable[] row = GetRow(obj);

            string tableName = obj.GetType().Name;

            InsertResult result = _interpreter.RunInsert(row, tableName);

            PropertyInfo identityProperty = GetIdentityColumn<T>();

            if (identityProperty != null)
            {
                identityProperty.SetValue(obj, result.IdentityValue);
            }
        }
    }
}
