namespace EventBaseClient.HealthCheck;

public interface IHealthCheckService
{
    public Task CheckHealthAsync(CancellationToken stoppingToken);
}
