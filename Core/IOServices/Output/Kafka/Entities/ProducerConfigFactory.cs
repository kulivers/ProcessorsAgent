using System.Net;
using Confluent.Kafka;
using Localization;
using Localization.Libs;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OuputServices.Kafka.Entities
{
    public class ProducerConfigFactory
    {
        private static readonly string NoBootstrapServerSpecified = IOServicesRecources.NoBootstrapServerSpecified;
        public static readonly string WrongTypeOfFileNeedToBeYaml = UtilResources.WrongTypeOfFileNeedToBeYaml;

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

        public ProducerConfigFactory(string path) : this(FromYaml(path))
        {
        }

        public ProducerConfigFactory(ClientConfig clientConfig)
        {
            ClientConfig = clientConfig;
        }

        public ProducerConfig GetDefaultProducerConfig()
        {
            if (ClientConfig.BootstrapServers == null)
            {
                throw new Exception(NoBootstrapServerSpecified);
            }

            ClientConfig.Acks ??= Acks.All;
            ClientConfig.ClientId ??= Dns.GetHostName();
            var producerConfig = new ProducerConfig(ClientConfig)
            {
                Partitioner = Partitioner.Consistent,
                QueueBufferingMaxMessages = 10000000
            };
            return producerConfig;
        }
    }
}