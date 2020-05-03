using SharpDb.Models;
using System;

namespace SharpDb.Services.Parsers
{
    public class GeneralParser
    {
        public string ToLowerAndTrim(string query)
        {
            query = query.Trim();

            query = query.ToLower();

            return query;
        }

        public string GetSqlStatementType(string query)
        {
            query = ToLowerAndTrim(query);

            return TruncateLongString(query, 6);
        }

        public string TruncateLongString(string str, int maxLength)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            return str.Substring(0, Math.Min(str.Length, maxLength));
        }

        public InnerStatement GetFirstMostInnerParantheses(string query)
        {
            int? indexOfLastOpeningParantheses = null;
            int? indexOfClosingParantheses = null;

            for (int i = 0; i < query.Length; i++)
            {
                if (query[i] == '(')
                {
                    indexOfLastOpeningParantheses = i;
                }

                if (query[i] == ')' && indexOfLastOpeningParantheses != null)
                {
                    indexOfClosingParantheses = i;
                    break;
                }
            }

            if (!indexOfLastOpeningParantheses.HasValue)
            {
                return null;
            }


            string subQuery = query.Substring((int)indexOfLastOpeningParantheses + 1, (int)(indexOfClosingParantheses - indexOfLastOpeningParantheses - 1));

            return new InnerStatement
            {
                Statement = subQuery,
                StartIndexOfOpenParantheses = (int)indexOfLastOpeningParantheses,
                EndIndexOfCloseParantheses = (int)indexOfClosingParantheses
            };
        }

        public InnerStatement GetOuterMostParantheses(string query)
        {
            int? indexFirstParantheses = null;
            int? indexOfClosingParantheses = null;

            for (int i = 0; i < query.Length; i++)
            {
                if (query[i] == '(')
                {
                    indexFirstParantheses = i;
                    break;
                }
            }

            for (int i = query.Length - 1; i >= 0; i--)
            {
                if (query[i] == ')')
                {
                    indexOfClosingParantheses = i;
                    break;
                }
            }

            if (!indexFirstParantheses.HasValue)
            {
                return null;
            }


            string subQuery = query.Substring((int)indexFirstParantheses + 1, (int)(indexOfClosingParantheses - indexFirstParantheses - 1));

            return new InnerStatement
            {
                Statement = subQuery,
                StartIndexOfOpenParantheses = (int)indexFirstParantheses,
                EndIndexOfCloseParantheses = (int)indexOfClosingParantheses
            };
        }
    }
}
