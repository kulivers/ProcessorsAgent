using Localization.Libs;

namespace Processor.Api.Exceptions;

public class ElasticSearchNotHealthyException : Exception
{
    private static readonly string ElasticSearchNotHealthy = ElasticClientResources.ElasticSearchNotHealthy;

    public ElasticSearchNotHealthyException(string responseContent) : base(string.Format(ElasticSearchNotHealthy, responseContent))
    {
        
    }
}

public class ElasticSearchNotAvailableException : Exception
{
    private static readonly string ElasticSearchNotAvailable = ElasticClientResources.ElasticSearchNotAvailable;

    public ElasticSearchNotAvailableException(string host, int port, int seconds) : base(
        string.Format(ElasticSearchNotAvailable, host, port, seconds))
    {
    }
}