using Avro;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using EventBaseClient.Exceptions;
using EventBaseClient.HealthCheck;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventBaseClient.Consumer;

public abstract class EventBaseConsumerWorker<TKey, TValue> : BackgroundService
{
    private readonly IHealthCheckService _healthCheckService;

    private readonly ILogger<EventBaseConsumerWorker<TKey, TValue>> _logger;

    private readonly EventConsumerOptions _eventConsumerOptions;

    public EventBaseConsumerWorker(
        IHealthCheckService healthCheckService,
        ILogger<EventBaseConsumerWorker<TKey, TValue>> logger,
        IOptions<EventConsumerOptions> eventConsumerOptions)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
        _eventConsumerOptions = eventConsumerOptions.Value;
    }

    protected abstract Task ProcessEvent(ConsumeResult<TKey, TValue> eventResult, CancellationToken stoppingToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var eventConsumer = InitEventConsumer();
        eventConsumer.Subscribe(_eventConsumerOptions.TopicName);

        var backoffTime = _eventConsumerOptions.InitBackoff;
        var retryCount = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _healthCheckService.CheckHealthAsync(stoppingToken);
                var eventResult = eventConsumer.Consume(stoppingToken);
                await ProcessEvent(eventResult, stoppingToken);
                eventConsumer.Commit();
                backoffTime = _eventConsumerOptions.InitBackoff;
                retryCount = 0;
            }
            catch (BusinessException ex)
            {
                _logger.LogError(ex, ex.Message);
                eventConsumer.Commit();
            }
            catch (ConsumeException ex) when (ex.InnerException is AvroException)
            {
                _logger.LogError(ex, ex.Message);
                eventConsumer.Commit();
                // send to dead letter
            }
            catch (UnHealthyServiceException ex)
            {
                _logger.LogError(ex, ex.Message);
                await Task.Delay(backoffTime, stoppingToken);
                backoffTime = TimeSpan.FromTicks(
                    Math.Min(backoffTime.Ticks * 2, _eventConsumerOptions.MaxBackoff.Ticks)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message. Attempting retry.");
                if (retryCount++ >= _eventConsumerOptions.MaxRetry)
                {
                    _logger.LogError("Max retries reached. Sending to dead letter.");
                    // send to dead letter
                }
                await Task.Delay(_eventConsumerOptions.RetryInterval, stoppingToken);
            }
        }
    }

    private IConsumer<TKey, TValue> InitEventConsumer()
    {
        var schemaRegistry = new CachedSchemaRegistryClient(_eventConsumerOptions.SchemaRegistryConfig);
        _eventConsumerOptions.ConsumerConfig.EnableAutoCommit = false;
        return new ConsumerBuilder<TKey, TValue>(_eventConsumerOptions.ConsumerConfig)
            .SetValueDeserializer(new AvroDeserializer<TValue>(schemaRegistry).AsSyncOverAsync())
            .Build();
    }
}
