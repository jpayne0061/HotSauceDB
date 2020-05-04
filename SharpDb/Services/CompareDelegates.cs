using SharpDb.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpDb.Services
{
    public class CompareDelegates
    {
        public static bool IsMoreThan(IComparable data, object queryValue)
        {
            return data.CompareTo(queryValue) == 1;
        }

        public static bool IsLessThan(IComparable data, object queryValue)
        {
            return data.CompareTo(queryValue) == -1;
        }

        public static bool IsEqualTo(IComparable data, object queryValue)
        {
            return data.CompareTo(queryValue) == 0;
        }

        public static bool NotEqualTo(IComparable data, object queryValue)
        {
            return data.CompareTo(queryValue) != 0;
        }

        public static bool MoreThanOrEqualTo(IComparable data, object queryValue)
        {
            return data.CompareTo(queryValue) == 1 || data.CompareTo(queryValue) == 0;
        }

        public static bool LessThanOrEqualTo(IComparable data, object queryValue)
        {
            return data.CompareTo(queryValue) == -1 || data.CompareTo(queryValue) == 0;
        }

        public static bool Contains(IComparable data, object queryValue)
        {
            return ((HashSet<IComparable>)queryValue).Contains(data);
        }

    }
}
