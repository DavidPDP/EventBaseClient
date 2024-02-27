using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using EventBaseClient.Consumer;
using Confluent.Kafka;
using EventBaseClient.HealthCheck;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using EventBaseClient.Producer;

namespace EventBaseClient.Test;

public class EventFlowTests
{
    [Fact]
    public async Task Invalid_Event_Schema_Flow()
    {
        var healthCheckService = new Mock<IHealthCheckService>();
        var eventConsumerLogger = new Mock<ILogger<EventBaseConsumerWorker<Ignore, EventRecord>>>();
        var serviceScope = Setup(healthCheckService, eventConsumerLogger);

        var producer = serviceScope.ServiceProvider.GetRequiredService<EventProducerService1>();
        var consumer = serviceScope.ServiceProvider.GetRequiredService<EventConsumerWorker>();

        var userEvent = new User
        {
            name = "test",
            favorite_number = 1000,
            favorite_color = "blue"
        };
        await producer.ProduceEvent(null, userEvent);

        _ = Task.Run(() => consumer.StartAsync(CancellationToken.None));
        await Task.Delay(5000);
        await consumer.StopAsync(CancellationToken.None);

        eventConsumerLogger.Verify(x => x.Log(
            LogLevel.Error,
            0,
            It.Is<It.IsAnyType>((v, t) => v.ToString() == "Local: Value deserialization error"),
            It.Is<Exception>(e => e is ConsumeException),
            (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
        Times.Once);
    }

    [Fact]
    public void Unhealthy_Services_Flow()
    {

    }

    [Fact]
    public void Business_Exception_Flow()
    {

    }

    [Fact]
    public void Unknow_Exception_Flow()
    {

    }

    public class EventConsumerWorker : EventBaseConsumerWorker<Ignore, EventRecord>
    {
        public EventConsumerWorker(
            IHealthCheckService healthCheckService,
            ILogger<EventBaseConsumerWorker<Ignore, EventRecord>> logger,
            IOptions<EventConsumerOptions> eventConsumerOptions)
                : base(healthCheckService, logger, eventConsumerOptions)
        {
        }

        protected override Task ProcessEvent(ConsumeResult<Ignore, EventRecord> eventResult, CancellationToken stoppingToken)
        {
            throw new NotImplementedException(eventResult.Message.Value.ToString());
        }
    }

    public class EventProducerService1 : EventBaseProducerService<Null, User>
    {
        public EventProducerService1(
            ILogger<EventBaseProducerService<Null, User>> logger,
            IOptions<EventProducerOptions> eventProducerOptions)
                : base(logger, eventProducerOptions)
        {
        }
    }

    public class EventProducerService2 : EventBaseProducerService<Null, EventRecord>
    {
        public EventProducerService2(
            ILogger<EventBaseProducerService<Null, EventRecord>> logger,
            IOptions<EventProducerOptions> eventProducerOptions)
                : base(logger, eventProducerOptions)
        {
        }
    }

    private IServiceScope Setup(
        Mock<IHealthCheckService> healthCheckService, 
        Mock<ILogger<EventBaseConsumerWorker<Ignore, EventRecord>>> eventConsumerLogger)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("Test/appsettings.json")
            .Build();

        return new HostBuilder().ConfigureServices((hostContext, services) =>
        {
            services.AddSingleton(configuration);
            services.AddSingleton<EventConsumerWorker>();
            services.AddSingleton<EventProducerService1>();
            services.AddSingleton<EventProducerService2>();
            services.AddSingleton(healthCheckService.Object);
            services.AddSingleton(eventConsumerLogger.Object);
            services.Configure<EventConsumerOptions>(configuration.GetSection("EventConsumerTest"));
            services.Configure<EventProducerOptions>(configuration.GetSection("EventProducerTest"));
        }).Build()
        .Services
        .CreateScope();
    }
}
