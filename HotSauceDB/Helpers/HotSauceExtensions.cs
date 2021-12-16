using HotSauceDb;
using HotSauceDB.Attributes;
using HotSauceDB.Statics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HotSauceDB.Helpers
{
    public static class HotSauceExtensions
    {
        public static List<string> SplitOnWhiteSpace(this string query)
        {
            return query.Split(' ')
                .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Replace("\r\n", "")).ToList();
        }

        public static T2 GetValueIfKeyExists<T, T2>(this Dictionary<T, T2> dict, T key)
        {
            if(dict.ContainsKey(key))
            {
                return dict[key];
            }
            else
            {
                return default(T2);
            }
        }

        public static PropertyInfo[] GetSupportedProperties(this Type type)
        {
            PropertyInfo[] properties = type.GetProperties();

            var supportedTypes = Constants.TypeToTypeEnum.Keys;

            return properties.Where(x => supportedTypes.Contains(x.PropertyType)).ToArray();
        }

    }
}
