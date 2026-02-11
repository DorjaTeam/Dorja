namespace DorjaData
{
    public class SQLiteConfiguration
    {
        public string ConnectionString { get; set; }

        public SQLiteConfiguration(string connectionString)
        {
            ConnectionString = connectionString; 
        }
    }
}

