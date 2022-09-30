using Localization;

namespace Processor.Api.Exceptions;

internal class TopicNotAvailableException2 : Exception
{
    private static readonly string TopicNotAvailable = IOServicesRecources.TopicNotAvailable;
    
    public TopicNotAvailableException2(string topic, string reason) : base(string.Format(TopicNotAvailable, topic, reason))
    {
        
        
    }
}