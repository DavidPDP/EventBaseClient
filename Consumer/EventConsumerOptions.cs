using Confluent.Kafka;
using Confluent.SchemaRegistry;

namespace EventBaseClient.Consumer;

public class EventConsumerOptions
{
    public required string TopicName { get; set; }

    public required int MaxRetry { get; set; }

    public required TimeSpan RetryInterval { get; set; }

    public required TimeSpan InitBackoff { get; set; }

    public required TimeSpan MaxBackoff { get; set; }

    public required ConsumerConfig ConsumerConfig { get; set; }

    public required SchemaRegistryConfig SchemaRegistryConfig { get; set; }
}
