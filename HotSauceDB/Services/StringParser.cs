﻿using HotSauceDb.Enums;
using System;

namespace HotSauceDb.Services
{
    public class StringParser
    {
        public IComparable ConvertToType(string val, TypeEnum type)
        {
            switch(type)
            {
                case TypeEnum.Boolean:
                    return bool.Parse(val);
                case TypeEnum.Char:
                    return char.Parse(val);
                case TypeEnum.Decimal:
                    return decimal.Parse(val);
                case TypeEnum.Int32:
                    return Int32.Parse(val);
                case TypeEnum.Int64:
                    return Int64.Parse(val);
                case TypeEnum.String:
                    return val.Trim('\'');
                case TypeEnum.DateTime:
                    return DateTime.Parse(val.Trim('\''));
                default:
                    throw new Exception($"no enum found for {type}");
            }
        }
    }
}
