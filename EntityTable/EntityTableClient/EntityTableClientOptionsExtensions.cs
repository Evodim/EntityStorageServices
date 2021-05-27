namespace EntityTableService
{
    public static partial class EntityTableClientOptionsExtensions
    {
         
             public static EntityTableClientOptions SetTableName(this EntityTableClientOptions options, string tableName )
        {
             
             options.TableName=tableName;
            return options;
        }

        public static EntityTableClientOptions SetMaxBatchedInsertionTasks(this EntityTableClientOptions options, int maxBatchedInsertionTasks=1)
        {

            options.MaxBatchedInsertionTasks = maxBatchedInsertionTasks;
            return options;
        }
        public static EntityTableClientOptions SetMaxItemsPerInsertion(this EntityTableClientOptions options, int maxItemsPerInsertion = 1)
        {

            options.MaxItemsPerInsertion = maxItemsPerInsertion;
            return options;
        }
        public static EntityTableClientOptions SetConnectionString(this EntityTableClientOptions options, string connectionString)
        {

            options.ConnectionString = connectionString;
            return options;
        }
    }
}