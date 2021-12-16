using System;

namespace HotSauceDB.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RelatedEntity : Attribute
    {
        private readonly string _entityName;

        public RelatedEntity(string entityName)
        {
            _entityName = entityName;
        }

        public string EntityName 
        { 
            get
            {
                return _entityName;
            }
        }

    }
}
