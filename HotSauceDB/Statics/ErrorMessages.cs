﻿namespace HotSauceDB.Statics
{
    public static class ErrorMessages
    {
        public static string Update_Missing_Identity = "Updating an object using this method requires that the entity have an identity column. " +
                                                        "As an alternative, you can update your records using the 'ProcessRawQuery' on the executor object";

        public static string Query_Must_Start_With = "Invalid query.Query must start with 'select' or 'insert'";
        public static string Three_Column_Max_In_Order_By = "There is a maximum of three columns allowed in the order by clause.";
        public static string One_Column_Max_In_Group_By = "Only one group by column allowed";
        public static string String_Column_Attribute_Missing(string x) => $@"Failed to get string length from property '{x}'. String properties
must have a StringLength attribute. Example:

[StringLength(20)] 
public string Name";
        public static string NO_RELATED_ENTITY_FOUND(string x) => $@"Could not find related entity '{x}'. Make sure you have a RelatedEntity attribute on the property you'd like to join on, like so:  [RelatedEntity('${x}')]";

    }
    
}
