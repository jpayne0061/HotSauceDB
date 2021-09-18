namespace HotSauceDB.Statics
{
    public static class ErrorMessages
    {
        public static string Update_Missing_Identity = "Updating an object using this method requires that the entity have an identity column. " +
                                                        "As an alternative, you can update your records using the 'ProcessRawQuery' on the executor object";
    }
}
