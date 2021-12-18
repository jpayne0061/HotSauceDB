using HotSauceDb.Models;
using HotSauceDb.Services;
using HotSauceDB.Helpers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;
using HotSauceDB.Statics;

namespace HotSauceDbOrm.Operations
{
    public class Insert : OperationsBase
    {
        public Insert(Interpreter interepreter) : base(interepreter) { }

        public IComparable InsertRow(object obj)
        {
            IComparable identityValue = InsertObject(obj);

            InsertChildren(obj, identityValue);

            return identityValue;
        }

        public void InsertChildren(object parentObject, IComparable parentId)
        {
            string parentName = parentObject.GetType().Name.ToLower();

            Dictionary<string, PropertyInfo> relatedEntities = HotSauceHelpers.GetRelatedEntityNames(parentObject.GetType());

            foreach (KeyValuePair<string, PropertyInfo> relatedEntity in relatedEntities)
            {
                IEnumerable childObjects = (IEnumerable)relatedEntity.Value.GetValue(parentObject);

                if(childObjects == null)
                {
                    continue;
                }

                foreach (object childObject in childObjects)
                {
                    string parentPropertyIdName = parentName + "id";

                    BindingFlags bindingFlags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;

                    PropertyInfo parentIdProperty = childObject.GetType().GetProperty(parentPropertyIdName, bindingFlags);

                    if(parentIdProperty == null)
                    {
                        throw new Exception(ErrorMessages.PARENT_ID_COLUMN_MISSING(parentPropertyIdName, childObject.GetType().Name));
                    }

                    parentIdProperty.SetValue(childObject, parentId);

                    IComparable identity = InsertObject(childObject);

                    InsertChildren(childObject, identity);
                }
            }
        }

        private IComparable InsertObject(object obj)
        {
            IComparable[] row = GetRow(obj);

            string tableName = obj.GetType().Name;

            InsertResult result = _interpreter.RunInsert(row, tableName);

            PropertyInfo identityProperty = GetIdentityColumn(obj);

            if (identityProperty != null)
            {
                identityProperty.SetValue(obj, result.IdentityValue);
            }

            return result.IdentityValue;
        }
    }
}
