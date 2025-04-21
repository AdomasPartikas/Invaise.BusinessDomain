namespace Invaise.BusinessDomain.API.Enums;
public enum AvailableServices
{
    /// <summary>
    /// Represents the market data service.
    /// </summary>
    [System.Runtime.Serialization.EnumMember(Value = "MarketDataService")]
    MarketDataService,

    /// <summary>
    /// Represents the data service.
    /// </summary>
    [System.Runtime.Serialization.EnumMember(Value = "DataService")]
    DataService,

    /// <summary>
    /// Represents the kaggle service.
    /// </summary>
    [System.Runtime.Serialization.EnumMember(Value = "KaggleService")]
    KaggleService,

    /// <summary>
    /// Represents the entirity of businessdomain.api application.
    /// </summary>
    [System.Runtime.Serialization.EnumMember(Value = "BusinessDomainAPI")]
    BusinessDomainAPI
}