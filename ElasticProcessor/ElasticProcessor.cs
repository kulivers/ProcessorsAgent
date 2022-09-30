using Localization.Processors;
using Processor;
using Processor.Api;
using Processor.Api.Exceptions;

namespace ElasticClient;

[ProcessElement(nameof(ElasticProcessor), ProcessingAttributeBehaviourType.Processor)]
public class ElasticProcessor : IProcessor<EsRequest, EsResponse>
{
    private readonly EsClient _esClient;

    private readonly string NotSupportedConfigType = ProcessorResources.NotSupportedConfigType;
    public string Name => ProcessorConfig.Name;
    public ProcessorConfig ProcessorConfig { get; }
    public double SecondsToResponse => 5;

    private const int MessagesPerSecondLimit = 1;
    private int MessagesPerSecond { get; set; }

    private bool AllowedToSend { get; set; }

    public ElasticProcessor(ProcessorConfig config)
    {
        MessagesPerSecond = 0;
        if (config.ConfigType != ConfigType.Yaml)
        {
            var possibleConfigTypes = string.Join(", ", Enum.GetValues(typeof(ConfigType)));
            var exInfo = string.Format(NotSupportedConfigType, possibleConfigTypes);
            throw new NotSupportedException(exInfo);
        }

        ProcessorConfig = config;
        var clientConfig = EsClientConfig.FromYaml(config.Config);
        _esClient = new EsClient(clientConfig);
        //если очередь не заполенена - отдаем сразу
        //если заполнена отправляем частями - у нас троттлер должен иметь воркер который постоянно будет инвоукать метод ниже через каждую секунду
        //2. сделать таску которая await возращает дату toProcess, и в форе ее тут обрабатывать
        var timer = new Timer(_ =>
        {
            MessagesPerSecond = 0;
            AllowedToSend = true;
        }, null, TimeSpan.Zero, new TimeSpan(0, 0, 1));
    }

    public async void CheckHealth()
    {
        await _esClient.CheckElasticAvailable(SecondsToResponse);
    }

    public ProcessorOutput<EsResponse> Process(EsRequest value, CancellationToken token)
    {
        var result = ProcessAsync(value, token).Result;
        return result;
    }

    public async Task<ProcessorOutput<EsResponse>> ProcessAsync(EsRequest value, CancellationToken token)
    {
        MessagesPerSecond++;
        if (MessagesPerSecond > MessagesPerSecondLimit)
        {
            AllowedToSend = false;
            while (true)
            {
                if (AllowedToSend)
                {
                    break;
                }
            }
        }

        try
        {
            var response = await _esClient.WriteRecordAsync(value, token);
            var output = new ProcessorOutput<EsResponse>(response);
            return output;
        }
        catch (TaskCanceledException ex)
        {
            var bigDelayFromElasticException = new TooBigDelayFromElasticException(value.HostConfig.Host);
            var output = new ProcessorOutput<EsResponse>(bigDelayFromElasticException);
            return output;
        }
        catch (Exception e)
        {
            var output = new ProcessorOutput<EsResponse>(e);
            return output;
        }
    }

    public TOut Process<TIn, TOut>(TIn value, CancellationToken token)
    {
        if (value is EsRequest esRequest)
        {
            var response = Process(esRequest, token);
            if (response is TOut castedResponse)
                return castedResponse;
        }

        throw new InvalidCastException();
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode() ^ ProcessorConfig.GetHashCode();
    }
}

internal class MessageThrottler
{
    private Queue<string> AllMessages { get; set; }
    private Queue<string> ToProcessMessages { get; set; }
    private TimeSpan TimeToFillProcessMessages { get; set; }

    private int Limit { get; set; }

    public MessageThrottler()
    {
        AllMessages = new Queue<string>();
        ToProcessMessages = new Queue<string>();
        TimeToFillProcessMessages = new TimeSpan(0, 0, 1);
        Limit = 3;
        var timer = new Timer(_ => { MoveToProcessMessages(); }, null, TimeSpan.Zero, TimeToFillProcessMessages);
    }


    private void MoveToProcessMessages()
    {
        while (AllMessages.TryDequeue(out string toProcess) && ToProcessMessages.Count >= Limit)
        {
            ToProcessMessages.Enqueue(toProcess);
        }
    }

    public void Enqueue(string data)
    {
        if (ToProcessMessages.Count < Limit)
        {
            ToProcessMessages.Enqueue(data);
        }
        else
        {
            AllMessages.Enqueue(data);
        }
    }

    public bool Dequeue(out string? result)
    {
        var canDequeue = ToProcessMessages.TryDequeue(out var toDequeue);
        result = toDequeue ?? throw new InvalidOperationException("Something goes wrong");
        return canDequeue;
    }

    public async Task<Queue<string>> DequeueAsync()
    {
        await Task.Delay(TimeToFillProcessMessages);
        return ToProcessMessages;
    }
}