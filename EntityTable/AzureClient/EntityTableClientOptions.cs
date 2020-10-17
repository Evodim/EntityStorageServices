namespace Evod.Toolkit.Azure.Storage
{
    public class EntityTableClientOptions
    {
        public EntityTableClientOptions()
        {
        }

        public EntityTableClientOptions(string connectionString, string tableName, int maxConcurrentInsertionTasks = 20, int maxItemsPerInsertion = 2000)
        {
            ConnectionString = connectionString;
            TableName = tableName;
            MaxBatchedInsertionTasks = maxConcurrentInsertionTasks;
            MaxItemsPerInsertion = maxItemsPerInsertion;
        }

        public string ConnectionString { get; set; }
        public string TableName { get; set; }
        public int MaxBatchedInsertionTasks { get; set; }
        public int MaxItemsPerInsertion { get; set; }
    }
}