using HotSauceDb.Enums;
using System;
using System.Collections.Generic;

namespace HotSauceDb
{
    public static class Constants
    {
        public const string FILE_NAME = "HotSauceDb.hdb";
        public const string IDENTITY_MARKER = "identity";
        public const short TABLE_DEF_LENGTH = 550;
        public const int Page_Size = 8000;
        public const long Next_Pointer_Address = 7992;
        public const long Max_Columns = 100;

        //24 bytes each column, 20 columns max = 500. + 41 bytes for name (string length 20)
        //plus 8 bytes for data location
        //plus 1 byte for is identity
        public const int PAGE_DATA_MAX = Page_Size - (Int64_Byte_Length + Int16_Byte_Length);

        public const short Boolean_Byte_Length = 1;
        public const short Char_Byte_Length = 1;
        public const short Decimal_Byte_Length = 16;
        public const short Int16_Byte_Length = 2;
        public const short Int32_Byte_Length = 4;
        public const short Int64_Byte_Length = 8;
        public const string Internal_Table_Name = "disk";
        public const char End_Table_Definition = '|';

        public static readonly HashSet<string> Predicate_Trailers = new HashSet<string> { "order", "group" };
        public static readonly HashSet<string> Aggregate_Functions = new HashSet<string> { "max", "min", "count" };

        public static readonly Dictionary<Type, TypeEnum> TypeToTypeEnum = new Dictionary<Type, TypeEnum>
        {
            {typeof(bool),     TypeEnum.Boolean},
            {typeof(char),     TypeEnum.Char},
            {typeof(DateTime), TypeEnum.DateTime},
            {typeof(decimal),  TypeEnum.Decimal},
            {typeof(Int32),    TypeEnum.Int32},
            {typeof(Int64),    TypeEnum.Int64},
            {typeof(string),   TypeEnum.String}
        };
    }
}
