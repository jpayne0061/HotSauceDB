using System.Collections.Generic;

namespace SharpDb
{
    public static class Globals
    {
        public const string FILE_NAME = "sharpDb.sdb";
        public static byte INDEX_PAGE_HEADER_LENGTH = 32;
        public const short TABLE_DEF_LENGTH = 530;
        public const int PageSize = 8000;
        public const long NextPointerAddress = 7992;
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
        public static readonly HashSet<string> AggregateFunctions = new HashSet<string> { "max", "min", "avg" };

        public const string InternalTableName = "disk";
    }
}
