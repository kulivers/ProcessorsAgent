using Localization.Libs;
using Localization.Processors;

namespace Processor.Api.Exceptions;

public class TooBigDelayFromElasticException : Exception
{
    private static readonly string TooBigDelayFromElastic = ElasticClientResources.TooBigDelayFromElastic;
    public TooBigDelayFromElasticException(string host) : base(string.Format(TooBigDelayFromElastic, host))
    {
        
    }
}
public class UnknownProcessorException : Exception
{
    private static readonly string UnknownProcessor = ProcessorResources.UnknownProcessor;

    public UnknownProcessorException(string name) : base(string.Format(UnknownProcessor, name))
    {
        
    }
}
public class CantLoadServiceException : Exception
{
    private static readonly string CantLoadService = ProcessorResources.CantLoadService;
    

    public CantLoadServiceException(string service) : base(string.Format(CantLoadService, service))
    {
        
    }
}