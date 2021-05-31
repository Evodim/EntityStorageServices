namespace EntityTableService
{
    public static partial class EntityTableClientOptionsExtensions
    { 
        public static EntityTableClientOptions SetTableName(this EntityTableClientOptions options, string value )
        { 
             options.TableName= value;
            return options;
        }

        public static EntityTableClientOptions SetMaxBatchedInsertionTasks(this EntityTableClientOptions options, int value = 1)
        {

            options.MaxBatchedInsertionTasks = value;
            return options;
        }
        public static EntityTableClientOptions SetMaxItemsPerInsertion(this EntityTableClientOptions options, int value = 1)
        {

            options.MaxItemsPerInsertion = value;
            return options;
        }
        public static EntityTableClientOptions SetConnectionString(this EntityTableClientOptions options, string value)
        {

            options.ConnectionString = value;
            return options;
        }
    }
}