using ProcessorsRunner;

class Program
{
    public static async Task Main(string[] args)
    {
        var agentYaml = @"D:\Work\ProcessorsAgent\SuperAgent\config\agent.yaml";
        var cfg = AgentConfig.FromYaml(agentYaml);
        var agent = new SuperAgent(cfg);
        await agent.Start();
    }
}