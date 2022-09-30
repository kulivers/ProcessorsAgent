using Confluent.Kafka;
using Localization.Libs;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace InputServices;

public class KafkaOutputConfig
{
    public IEnumerable<string> Topics { get; set; }
    public ClientConfig Client { get; set; }
    public static readonly string WrongTypeOfFileNeedToBeYaml = UtilResources.WrongTypeOfFileNeedToBeYaml;

    public KafkaOutputConfig()
    {
        
    }
    public KafkaOutputConfig(string path)
    {
        var config = FromYaml(path);
        Client = config.Client;
        Topics = config.Topics;
    }
    public static KafkaOutputConfig FromYaml(string path)
    {
        
        if (!path.EndsWith(".yaml"))
        {
            throw new ArgumentException(WrongTypeOfFileNeedToBeYaml);
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance) 
            .Build();
        var fileContent = File.ReadAllText(path);
        return deserializer.Deserialize<KafkaOutputConfig>(fileContent);

    }
    
}