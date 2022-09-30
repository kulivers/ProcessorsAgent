using Confluent.Kafka;
using Localization.Libs;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace KafkaInteractor
{
    public class ConsumerConfigFactory
    {
        private const string DefaultGroupId = "foo";
        private static readonly string WrongTypeOfFileNeedToBeYaml = UtilResources.WrongTypeOfFileNeedToBeYaml;
        private ClientConfig ClientConfig { get; }

        private static ClientConfig FromYaml(string path)
        {
            if (!path.EndsWith(".yaml"))
            {
                throw new ArgumentException(WrongTypeOfFileNeedToBeYaml);
            }
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance) 
                .Build();
            var fileContent = File.ReadAllText(path);
            return deserializer.Deserialize<ClientConfig>(fileContent);
        }

        public ConsumerConfigFactory(ClientConfig clientConfig)
        {
            ClientConfig = clientConfig;
        }

        public ConsumerConfig GetDefaultConsumerConfig()
        {
            var consumerConfig = new ConsumerConfig(ClientConfig);
            consumerConfig.GroupId ??= DefaultGroupId;
            consumerConfig.AutoOffsetReset ??= AutoOffsetReset.Latest;
            consumerConfig.EnableAutoCommit = false;
            return consumerConfig;
        }
    }
}