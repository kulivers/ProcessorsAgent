using Confluent.Kafka;
using Localization.Libs;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace InputServices;

public class KafkaInputConfig
{
    public IEnumerable<string> Topics { get; set; }
    public ClientConfig Client { get; set; }
    public static readonly string WrongTypeOfFileNeedToBeYaml = UtilResources.WrongTypeOfFileNeedToBeYaml;

    public KafkaInputConfig()
    {
        //for mapping from config file
    }

    public KafkaInputConfig(string path)
    {
        var config = FromYaml(path);
        Client = config.Client;
        Topics = config.Topics;
    }

    public static KafkaInputConfig FromYaml(string path)
    {
        if (!path.EndsWith(".yaml"))
        {
            throw new ArgumentException(WrongTypeOfFileNeedToBeYaml);
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var fileContent = File.ReadAllText(path);
        return deserializer.Deserialize<KafkaInputConfig>(fileContent);
    }
}