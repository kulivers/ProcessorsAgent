using InputServices;
using IOServices.Api;
using OuputServices;
using Processor.Api.Exceptions;

namespace ProcessorsRunner;

public class ConnectorFactory
{
    public IConnector CreateConnector(ConnectorConfig config)
    {
        var destination = config.Destination;
        IInputService? inputService = null;
        IOutputService? outputService = null;

        if (config.Input == InputService.Kafka)
        {
            var inputConfig = new KafkaInputConfig(config.InputConfig);
            inputService = new KafkaInput(inputConfig);
        }

        if (config.Output == OutputService.Kafka)
        {
            if (config.OutputConfig == null)
            {
                throw new ConfigCantBeNullException();
            }
            
            var outputConfig = new KafkaOutputConfig(config.OutputConfig);
            outputService = new KafkaOutputService(outputConfig);
        }

        if (inputService == null)
        {
            throw new InputServiceCantBeNullException();
        }

        return new Connector(destination, inputService, outputService);
    }
}