using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;

namespace EventBaseClient.Producer;

public class EventProducerOptions
{
    public required string TopicName { get; set; }

    public ProducerConfig ProducerConfig { get; set; } = new ProducerConfig();

    public AvroSerializerConfig AvroSerializerConfig { get; set; } = new AvroSerializerConfig();

    public SchemaRegistryConfig SchemaRegistryConfig { get; set; } = new SchemaRegistryConfig();
}
