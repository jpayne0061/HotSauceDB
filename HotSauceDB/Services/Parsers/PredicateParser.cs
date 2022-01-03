using HotSauceDb.Enums;
using HotSauceDb.Models;
using HotSauceDb.Services.Parsers;
using HotSauceDb.Statics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HotSauceDB.Services.Parsers
{
    public class PredicateParser : GeneralParser
    {
        public List<PredicateOperation> BuildPredicateOperations(TableDefinition tableDefinition, List<string> predicates)
        {
            if (predicates == null || !predicates.Any())
            {
                return new List<PredicateOperation>();
            }

            var predicateOperations = new List<PredicateOperation>();

            var operatorToDelegate = new Dictionary<string, Func<IComparable, object, bool>>
                {
                    { ">",   CompareDelegates.IsMoreThan },
                    { "<",   CompareDelegates.IsLessThan},
                    { "=",   CompareDelegates.IsEqualTo},
                    { ">=",  CompareDelegates.MoreThanOrEqualTo},
                    { "<=",  CompareDelegates.LessThanOrEqualTo},
                    { "!=",  CompareDelegates.NotEqualTo},
                    { "in",  CompareDelegates.Contains},
                };


            int operatorIndex = 0;
            int delegateIndex = 2;
            int innerValueIndex = 3;
            object comparingValue = null;

            for (int i = 0; i < predicates.Count(); i++)
            {
                List<string> predicateParts = GetPredicateParts(predicates[i]);

                predicateParts = CombineValuesInParantheses(predicateParts);

                var columnDefintion = tableDefinition.ColumnDefinitions.FirstOrDefault(x => x.ColumnName == predicateParts[1].ToLower());

                if (columnDefintion == null)
                {
                    throw new Exception($"Unknown column in where clause: {predicateParts[1]}");
                }

                try
                {
                    if (string.Equals(predicateParts[delegateIndex], "in", StringComparison.OrdinalIgnoreCase))
                    {
                        comparingValue = GetInStatementValue(predicateParts, columnDefintion);
                    }
                    else
                    {
                        comparingValue = ConvertToType(columnDefintion, predicateParts[innerValueIndex]);
                    }

                    predicateOperations.Add(new PredicateOperation
                    {
                        Delegate = operatorToDelegate[predicateParts[delegateIndex].ToLower()],
                        Value = comparingValue,
                        Operator = predicateParts[operatorIndex],
                        ColumnIndex = columnDefintion.Index
                    });
                }
                catch (KeyNotFoundException)
                {
                    throw new Exception($"Illegal operator: {predicateParts[2]}");
                }
            }

            return predicateOperations;
        }

        private List<string> GetPredicateParts(string predicatePart)
        {
            string[] predicateSplitOnTicks = predicatePart.Split("'");

            List<string[]> pieces = new List<string[]>();

            for (int i = 0; i < predicateSplitOnTicks.Length; i++)
            {
                if (i % 2 == 0)
                {
                    pieces.Add(predicateSplitOnTicks[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                }
                else
                {
                    pieces.Add(new string[] { "'" + predicateSplitOnTicks[i] + "'" });
                }
            }

            return pieces.SelectMany(element => element).Select(x => x.Replace("\r\n", string.Empty)).ToList();
        }

        private object GetInStatementValue(List<string> predicateParts, ColumnDefinition colDef)
        {
            string innerValue = predicateParts[3].Trim(new char[] { '(', ')' });

            var list = new List<string>(innerValue.Split(','));

            return list.Select(x => ConvertToType(colDef, x)).ToHashSet();
        }

        private IComparable ConvertToType(ColumnDefinition columnDefinition, string val)
        {
            IComparable convertedVal;

            switch (columnDefinition.Type)
            {
                case TypeEnum.Boolean:
                    convertedVal = Convert.ToBoolean(val);
                    break;
                case TypeEnum.Char:
                    convertedVal = Convert.ToChar(val);
                    break;
                case TypeEnum.Decimal:
                    convertedVal = Convert.ToDecimal(val);
                    break;
                case TypeEnum.Int32:
                    convertedVal = Convert.ToInt32(val);
                    break;
                case TypeEnum.Int64:
                    convertedVal = Convert.ToInt64(val);
                    break;
                case TypeEnum.String:
                    convertedVal = val.TrimStart('\'').TrimEnd('\'').PadRight(columnDefinition.ByteSize - 1, ' ');
                    break;
                case TypeEnum.DateTime:
                    convertedVal = Convert.ToDateTime(val.TrimStart('\'').TrimEnd('\''));
                    break;
                default:
                    convertedVal = null;
                    break;
            }

            return convertedVal;
        }

    }
}
