namespace HotSauceDB.Statics
{
    public static class ErrorMessages
    {
        public static string Update_Missing_Identity = "Updating an object using this method requires that the entity have an identity column. " +
                                                        "As an alternative, you can update your records using the 'ProcessRawQuery' on the executor object";

        public static string Query_Must_Start_With = "Invalid query.Query must start with 'select' or 'insert'";
        public static string Three_Column_Max_In_Order_By = "There is a maximum of three columns allowed in the order by clause.";
        public static string One_Column_Max_In_Group_By = "Only one group by column allowed";
    }
}
