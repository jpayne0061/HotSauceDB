using HotSauceDB.Attributes;
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

                Dictionary<string, PropertyInfo> relatedEntityMapping = GetRelatedEntityNames(typeof(T));

                PropertyInfo pi = relatedEntityMapping[relatedManyEntityName];

                pi.SetValue(parentObject, includedObject);
            }

            return parentObjectList;
        }

        private static Dictionary<string, PropertyInfo> GetRelatedEntityNames(Type type)
        {
            var relatedEntityNames = new Dictionary<string, PropertyInfo>();

            foreach (PropertyInfo propertyInfo in type.GetProperties())
            {
                if (propertyInfo.CustomAttributes.Any(x => x.AttributeType == typeof(RelatedEntity)))
                {
                    Attribute relatedAttribute = propertyInfo.GetCustomAttribute(typeof(RelatedEntity));

                    string relatedEntityName = ((RelatedEntity)relatedAttribute).EntityName;

                    if (string.IsNullOrWhiteSpace(relatedEntityName))
                    {
                        throw new Exception(ErrorMessages.RELATED_ATTRIBUTE_IS_MISSING(propertyInfo.Name));
                    }

                    relatedEntityNames[relatedEntityName] = propertyInfo;
                }
            }

            return relatedEntityNames;
        }
    }
}
