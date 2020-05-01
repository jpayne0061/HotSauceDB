using SharpDb.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpDb.Services
{
    public class CompareDelegates
    {
        public static bool IsMoreThan(IComparable data, IComparable queryValue)
        {
            return data.CompareTo(queryValue) == 1;
        }

        public static bool IsLessThan(IComparable data, IComparable queryValue)
        {
            return data.CompareTo(queryValue) == -1;
        }

        public static bool IsEqualTo(IComparable data, IComparable queryValue)
        {
            return data.CompareTo(queryValue) == 0;
        }

        public static bool NotEqualTo(IComparable data, IComparable queryValue)
        {
            return data.CompareTo(queryValue) != 0;
        }

        public static bool MoreThanOrEqualTo(IComparable data, IComparable queryValue)
        {
            return data.CompareTo(queryValue) == 1 || data.CompareTo(queryValue) == 0;
        }

        public static bool LessThanOrEqualTo(IComparable data, IComparable queryValue)
        {
            return data.CompareTo(queryValue) == -1 || data.CompareTo(queryValue) == 0;
        }

    }
}
