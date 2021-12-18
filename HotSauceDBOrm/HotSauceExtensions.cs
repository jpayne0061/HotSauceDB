using HotSauceDb.Models;
using HotSauceDB.Attributes;
using HotSauceDB.Helpers;
using HotSauceDB.Statics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HotSauceDbOrm
{
    public static class HotSauceExtensions
    {
        public static List<T> Include<T, T1>(this List<T> parentObjectList) where T : new() where T1 : new()
        {
            string parentEntityName = typeof(T).Name;

            string relatedManyEntityName = typeof(T1).Name;

            PropertyInfo parentEntityIdentityColumn = typeof(T).GetProperties()
                    .Where(x => x.Name.ToLower() == parentEntityName.ToLower() + "id").FirstOrDefault();

            if(parentEntityIdentityColumn == null)
            {
                throw new Exception(ErrorMessages.IDENTITY_COLUMN_IS_MISSING(parentEntityName));
            }

            foreach (var parentObject in parentObjectList)
            {
                IComparable val = (IComparable)parentEntityIdentityColumn.GetValue(parentObject);

                string query = $"select * from {relatedManyEntityName} where {parentEntityName}id = {val}";

                List<T1> includedObject = Executor.GetInstance().Read<T1>(query);

                Dictionary<string, PropertyInfo> relatedEntityMapping = HotSauceHelpers.GetRelatedEntityNames(typeof(T));

                PropertyInfo pi = relatedEntityMapping[relatedManyEntityName];

                pi.SetValue(parentObject, includedObject);
            }

            return parentObjectList;
        }

        public static bool IsIdentity<T>(this PropertyInfo propertyInfo)
        {
            bool hasIdentityAttribute = propertyInfo.CustomAttributes.Any(x => x.AttributeType == typeof(Identity));
            bool usesIdentityConvention = propertyInfo.Name.ToLower() == typeof(T).Name.ToLower() + "id";

            return hasIdentityAttribute || usesIdentityConvention;
        }

        public static bool IsIdentity(this PropertyInfo propertyInfo, Type type)
        {
            bool hasIdentityAttribute = propertyInfo.CustomAttributes.Any(x => x.AttributeType == typeof(Identity));
            bool usesIdentityConvention = propertyInfo.Name.ToLower() == type.Name.ToLower() + "id";

            return hasIdentityAttribute || usesIdentityConvention;
        }

        public static bool IsIdentity(this ColumnDefinition columnDefinition, string tableName)
        {
            return columnDefinition.ColumnName.ToLower() == tableName.ToLower() + "id";
        }
    }
}
