﻿using Confluent.Kafka;
using IOServices.Api;
using KafkaInteractor;
using Processor.Api.Exceptions;

namespace InputServices;

public class KafkaInput : IInputService, IDisposable
{
    
    private readonly IEnumerable<string> _inputTopics;
    private readonly ConsumerConfig _consumerConfig;
    private IConsumer<int, string> StringConsumer { get; }

    public KafkaInput(KafkaInputConfig config) : this(new ConsumerConfigFactory(config.Client).GetDefaultConsumerConfig(), config.Topics)
    {
    }

    public KafkaInput(ConsumerConfig consumerConfig, IEnumerable<string> inputTopics)
    {
        _inputTopics = inputTopics;
        _consumerConfig = consumerConfig;
        var consumerFactory = new ConsumerFactory(_consumerConfig);
        StringConsumer = consumerFactory.CreateStringConsumer();
    }

    private const int MessagesToCommit = 1000;
    public event EventHandler<object> OnReceive;
    

    public async Task StartReceive(CancellationToken token)
    {
        var messagesToCommit = MessagesToCommit;
        StringConsumer.Subscribe(_inputTopics);

        while (!token.IsCancellationRequested)
        {
            var received = StringConsumer.Consume(token);
            if (received == null)
            {
                continue;
            }

            var value = received.Message.Value;
            CallOnMessageEvent(value);

            messagesToCommit--;
            if (messagesToCommit <= 0)
            {
                StringConsumer.Commit();
                messagesToCommit = MessagesToCommit;
            }
        }
    }

    public void CheckHealth(double secondsToResponse)
    {
        var adminConfig = new AdminClientConfig(_consumerConfig);
        var adminClient = new AdminClientBuilder(adminConfig).Build();
        using (adminClient)
        {
            foreach (var topic in _inputTopics)
            {
                var metadata = adminClient.GetMetadata(topic,  TimeSpan.FromSeconds(secondsToResponse));
                foreach (var topicMetadata in metadata.Topics)
                {
                    if (topicMetadata.Error.IsError)
                    {   
                        throw new TopicNotAvailableException(topicMetadata.Topic, topicMetadata.Error.Reason);
                    }
                }
            }
        }
    }

    private protected virtual void CallOnMessageEvent(object recieved)
    {
        OnReceive?.Invoke(this, recieved);
    }

    public void Dispose()
    {
        StringConsumer.Dispose();
    }
}