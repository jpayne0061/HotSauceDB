using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HotSauceDB.Statics
{
    public static class HotSauceExtensions
    {
        public static List<string> SplitOnWhiteSpace(this string query)
        {
            return query.Split(' ')
                .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Replace("\r\n", "")).ToList();
        }

    }
}
