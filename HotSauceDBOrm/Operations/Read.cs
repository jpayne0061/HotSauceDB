using HotSauceDb.Models;
using HotSauceDb.Services;
using HotSauceDb.Services.Parsers;
using HotSauceDB.Attributes;
using HotSauceDB.Statics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HotSauceDbOrm.Operations
{
    public static class OrmExtensions 
    {
        public static List<T> Include<T, T1>(this List<T> entityList) where T : new() where T1 : new()
        {
            string parentEntityName = typeof(T).Name;

            string relatedManyEntityName = typeof(T1).Name;

            Executor executor = Executor.GetInstance();

            List<T1> entities = new List<T1>();

            RelatedEntity relatedColumn =
                (RelatedEntity)typeof(T1).GetProperties()
                .SelectMany(x => x.GetCustomAttributes(typeof(RelatedEntity)))
                .Where(x => parentEntityName == ((RelatedEntity)x).EntityName)
                .FirstOrDefault();

            var propertyNameToAttributeValue = new KeyValuePair<string, string>();

            foreach (PropertyInfo propertyInfo in typeof(T1).GetProperties())
            {
                if (propertyInfo.CustomAttributes.Any(x => x.AttributeType == typeof(RelatedEntity)))
                {
                    Attribute relatedAttribute = propertyInfo.GetCustomAttribute(typeof(RelatedEntity));

                    string relatedEntityName = ((RelatedEntity)relatedAttribute).EntityName;

                    if (parentEntityName == relatedEntityName)
                    {
                        propertyNameToAttributeValue = new KeyValuePair<string, string>(propertyInfo.Name, relatedEntityName);
                    }
                }
            }

            if (propertyNameToAttributeValue.Value == null)
            {
                throw new Exception(ErrorMessages.NO_RELATED_ENTITY_FOUND(parentEntityName));
            }

            foreach (var parentEntity in entityList)
            {
                PropertyInfo parentEntityIdentityColumn = typeof(T).GetProperties()
                    .Where(x => x.Name.ToLower() == parentEntityName.ToLower() + "id").FirstOrDefault();

                IComparable val = (IComparable)parentEntityIdentityColumn.GetValue(parentEntity);

                string query = $"select * from {relatedManyEntityName} where {relatedManyEntityName}id = {val}";

                entities = executor.Read<T1>(query);

                Dictionary<string, PropertyInfo> relatedEntityMapping = GetRelatedEntityNames(typeof(T));

                PropertyInfo pi = relatedEntityMapping[relatedManyEntityName];

                pi.SetValue(parentEntity, entities);
            }

            return entityList;
        }

        private static Dictionary<string, PropertyInfo> GetRelatedEntityNames(Type type)
        {
            var relatedEntityNames = new Dictionary<string, PropertyInfo>();

            foreach (PropertyInfo propertyInfo in type.GetProperties())
            {
                if (propertyInfo.CustomAttributes.Any(x => x.AttributeType == typeof(RelatedEntity)))
                {
                    Attribute relatedAttribute = propertyInfo.GetCustomAttribute(typeof(RelatedEntity));

                    string relatedEntityName = ((RelatedEntity)relatedAttribute).EntityName;

                    if (string.IsNullOrWhiteSpace(relatedEntityName))
                    {
                        throw new Exception($"Related Entity Attribute on property '${propertyInfo.Name}' is null or empty");
                    }

                    relatedEntityNames[relatedEntityName] = propertyInfo;
                }
            }

            return relatedEntityNames;
        }

    }

    public class Read : OperationsBase
    {   
        public Read(Interpreter interpreter) : base(interpreter)
        {
            _interpreter = interpreter;
        }

        

        public List<T> ReadRows<T>(string query) where T : new()
        {
            TableDefinition tableDef = _interpreter.GetTableDefinition(typeof(T).Name);

            List<List<IComparable>> rows = _interpreter.RunQueryAndSubqueries(query);

            Dictionary<int, string> indexToColumn = IndexToColumn(query, tableDef);

            List<T> transformedRows = new List<T>();

            foreach (var row in rows)
            {
                T t = new T();

                for (int i = 0; i < row.Count; i++)
                {
                    string columnName = indexToColumn[i];

                    TrySetProperty(t, columnName, row[i]);
                }

                transformedRows.Add(t);
            }

            return transformedRows;
        }


        

        private bool HasRelatedEntities<T>()
        {
            PropertyInfo[] properties = typeof(T).GetProperties();

            return properties.SelectMany(x => x.CustomAttributes)
                .Any(x => x.AttributeType == typeof(RelatedEntity));
        }

        private Dictionary<int, string> IndexToColumn(string query, TableDefinition tableDef)
        {
            Dictionary<int, string> selectIndexToColumnName = new Dictionary<int, string>();

            SelectParser selectParser = new SelectParser();

            List<SelectColumnDto> selectColumns = selectParser.GetColumns(query);

            if(selectColumns.First().ColumnName == "*")
            {
                return tableDef.ColumnDefinitions.ToDictionary(x => (int)x.Index, x => x.ColumnName);
            }

            for (int i = 0; i < selectColumns.Count; i++)
            {
                selectIndexToColumnName[i] = selectColumns[i].ColumnName;
            }

            return selectIndexToColumnName;
        }

        private bool TrySetProperty(object obj, string property, object value)
        {
            var prop = obj.GetType().GetProperty(property, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(obj, value, null);
                return true;
            }
            return false;
        }
    }
}
