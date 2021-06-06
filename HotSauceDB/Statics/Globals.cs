using System.Collections.Generic;

namespace HotSauceDb
{
    public static class Globals
    {
        public static int GLOBAL_DEBUG = 0;
        public const string FILE_NAME = "HotSauceDb.hdb";
        public static byte INDEX_PAGE_HEADER_LENGTH = 32;
        public const short TABLE_DEF_LENGTH = 530;
        public const int PageSize = 8000;
        public const long NextPointerAddress = 7992;
        public const int PAGE_DATA_MAX = PageSize - (Int64ByteLength + Int16ByteLength);
        //24 bytes each column, 20 columns max = 480. + 41 bytes for name (string length 20)
        //plus 8 bytes for data location

        public static char EndTableDefinition = '|';

        public const short BooleanByteLength = 1;
        public const short CharByteLength = 2;
        public const short DecimalByteLength = 16;
        public const short Int16ByteLength = 2;
        public const short Int32ByteLength = 4;
        public const short Int64ByteLength = 8;

        public static readonly HashSet<string> PredicateTrailers = new HashSet<string> { "order", "group" };
        public static readonly HashSet<string> AggregateFunctions = new HashSet<string> { "max", "min", "count" };

        public const string InternalTableName = "disk";
    }
}
