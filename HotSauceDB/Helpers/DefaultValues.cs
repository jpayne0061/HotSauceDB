using HotSauceDb.Enums;
using System;

namespace HotSauceDB.Helpers
{
    public static class DefaultValues
    {
        public static IComparable GetDefaultValueForType(TypeEnum typeEnum)
        {
            switch (typeEnum)
            {
                case TypeEnum.Boolean:
                    return false;
                case TypeEnum.Char:
                    return ' ';
                case TypeEnum.DateTime:
                case TypeEnum.Decimal:
                case TypeEnum.Int32:
                case TypeEnum.Int64:
                    return 0;
                case TypeEnum.String:
                    return "";
                default:
                    throw new Exception("Unsupported type");
            }
        }
    }
}
