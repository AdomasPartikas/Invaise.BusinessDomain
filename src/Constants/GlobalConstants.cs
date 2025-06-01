namespace Invaise.BusinessDomain.API.Constants
{
    /// <summary>
    /// Contains global constants used throughout the application.
    /// </summary>
    public static class GlobalConstants
    {
        //Datasets
        /// <summary>
        /// The Kaggle dataset identifier for SandP 500 daily updates.
        /// </summary>
        public const string KaggleSmpDataset = "yash16jr/s-and-p500-daily-update-dataset";

        //Folder paths
        /// <summary>
        /// The relative path to the data folder used in the application.
        /// </summary>
        public const string DataFolder = @"..\..\..\Data";

        //Dataset names
        /// <summary>
        /// The name of the raw SandP 500 dataset file.
        /// </summary>
        public const string SmpDatasetRaw = "SnP_daily_update.csv";
        /// <summary>
        /// The name of the cleaned SandP 500 dataset file.
        /// </summary>
        public const string SmpDataset = "SMP_Cleaned.csv";

        //Dataset constants
        /// <summary>
        /// The total number of tickers in the dataset.
        /// </summary>
        public const int TickerCount = 503;
        /// <summary>
        /// The number of price blocks used in the dataset.
        /// </summary>
        public const int PriceBlocks = 5;

        //Service constants
        /// <summary>
        /// The number of retry attempts for service calls.
        /// </summary>
        public const int Retries = 3;
        /// <summary>
        /// The delay in seconds between retry attempts for service calls.
        /// </summary>
        public const int RetryDelaySeconds = 50;

    }
}