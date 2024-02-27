using Confluent.Kafka;
using Confluent.SchemaRegistry.Serdes;
using Confluent.SchemaRegistry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventBaseClient.Producer;

public abstract class EventBaseProducerService<TKey, TValue>
{
    private readonly ILogger _logger;

    private readonly IProducer<TKey, TValue> _producer;

    private readonly EventProducerOptions _eventProducerOptions;

    public EventBaseProducerService(
        ILogger<EventBaseProducerService<TKey, TValue>> logger,
        IOptions<EventProducerOptions> eventProducerOptions)
    {
        _logger = logger;
        _eventProducerOptions = eventProducerOptions.Value;
        _producer = InitEventConsumer();
    }

    public async Task ProduceEvent(TKey eventKey, TValue eventMessage)
    {
        try
        {
            await _producer.ProduceAsync(
                _eventProducerOptions.TopicName,
                new Message<TKey, TValue>
                {
                    Key = eventKey,
                    Value = eventMessage
                }
            );
        } 
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

    private IProducer<TKey, TValue> InitEventConsumer()
    {
        var schemaRegistry = new CachedSchemaRegistryClient(_eventProducerOptions.SchemaRegistryConfig);
        return new ProducerBuilder<TKey, TValue>(_eventProducerOptions.ProducerConfig)
            .SetValueSerializer(new AvroSerializer<TValue>(schemaRegistry, _eventProducerOptions.AvroSerializerConfig))
            .Build();
    }
}
