using HotSauceDB.Attributes;
using HotSauceDB.Statics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HotSauceDB.Helpers
{
    public static class HotSauceHelpers
    {
        public static Dictionary<string, PropertyInfo> GetRelatedEntityNames(Type type)
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
