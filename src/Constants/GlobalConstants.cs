namespace Invaise.BusinessDomain.API.Constants
{
    public static class GlobalConstants
    {
        //Urls
        public const string KaggleUrl = "https://www.kaggle.com";
        public const string FinnhubUrl = "https://finnhub.io/api/v1/";

        //Datasets
        public const string KaggleSmpDataset = "yash16jr/s-and-p500-daily-update-dataset";

        //Folder paths
        public const string DataFolder = @"..\..\..\Data";

        //Dataset names
        public const string SmpDatasetRaw = "SnP_daily_update.csv";
        public const string SmpDataset = "SMP_Cleaned.csv";

        //Dataset constants
        public const int TickerCount = 503;
        public const int PriceBlocks = 5;

        //Service constants
        public const int Retries = 3;
        public const int RetryDelaySeconds = 50;

    }
}