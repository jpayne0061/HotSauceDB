using HotSauceDb.Models;
using HotSauceDb.Services;
using HotSauceDB.Statics;
using System;
using System.Linq;
using System.Reflection;

namespace HotSauceDbOrm.Operations
{
    public class Update : Insert
    {
        public Update(Interpreter interpreter) : base(interpreter) { }

        public void UpdateRecord<T>(T obj)
        {
            string tableName = typeof(T).Name;

            TableDefinition tableDefinition = _interpreter.GetTableDefinition(tableName);

            ColumnDefinition identityColumn = tableDefinition.ColumnDefinitions.Where(x => x.IsIdentity == 1).FirstOrDefault();

            if(identityColumn == null)
            {
                throw new Exception(ErrorMessages.Update_Missing_Identity);
            }

            PropertyInfo identityProperty = typeof(T).GetProperties().Where(x => x.Name.ToLower() == identityColumn.ColumnName.ToLower()).Single();

            IComparable identityValue = (IComparable)identityProperty.GetValue(obj);

            string sql = GetSqlUpdateStatement(tableDefinition.TableName, identityColumn.ColumnName, obj, identityValue);

            _interpreter.ProcessStatement(sql);
        }

        private string GetSqlUpdateStatement<T>(string tableName, string identityColumnName, T obj, IComparable identityValue)
        {
            string sql = $"update {tableName} set ";

            PropertyInfo[] propertyInfos = typeof(T).GetProperties();

            string[] setStatement = new string[propertyInfos.Length];

            for (int i = 0; i < propertyInfos.Length; i++)
            {
                if(propertyInfos[i].PropertyType == typeof(string) || propertyInfos[i].PropertyType == typeof(DateTime))
                {
                    setStatement[i] = propertyInfos[i].Name + " = '" + (IComparable)propertyInfos[i].GetValue(obj) + "'";
                }
                else
                {
                    setStatement[i] = propertyInfos[i].Name + " = " + (IComparable)propertyInfos[i].GetValue(obj);
                }
            }

            sql += string.Join(',', setStatement) + $" where {identityColumnName} = {identityValue}";

            return sql;
        }
    }
}
