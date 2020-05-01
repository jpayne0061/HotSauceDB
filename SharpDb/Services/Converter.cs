using SharpDb.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpDb.Services
{
    public class Converter
    {
        public IComparable ConvertToType(string val, TypeEnums type)
        {
            switch(type)
            {
                case TypeEnums.Boolean:
                    return bool.Parse(val);
                case TypeEnums.Char:
                    return char.Parse(val);
                case TypeEnums.Decimal:
                    return decimal.Parse(val);
                case TypeEnums.Int32:
                    return Int32.Parse(val);
                case TypeEnums.Int64:
                    return Int64.Parse(val);
                case TypeEnums.String:
                    return val.Trim('\'');
                default:
                    throw new Exception($"no enum found for {type.ToString()}");
            }
        }
    }
}
