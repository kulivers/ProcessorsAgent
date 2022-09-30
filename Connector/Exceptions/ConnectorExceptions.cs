using Localization;

namespace Processor.Api.Exceptions;

public class InputServiceCantBeNullException : Exception
{
    private static readonly string InputServiceCantBeNull = string.Format(ConnectorResources.InputServiceCantBeNull);
    public InputServiceCantBeNullException() : base(InputServiceCantBeNull)
    {
        
    }
}

public class ConfigCantBeNullException : Exception
{
    private static readonly string OutputConfigCantBeNull = ConnectorResources.OutputConfigCantBeNull;
    public ConfigCantBeNullException() : base(OutputConfigCantBeNull)
    {
        
    }
}