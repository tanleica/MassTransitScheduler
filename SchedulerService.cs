using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class SchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SchedulerService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // 🔹 Run every hour

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();

            var message = new OutboxMessage
            {
                UserId = "b2e05ec4-6022-4f35-baea-ceb7fa2ee9dd",
                Message = $"Scheduled Notification at {DateTime.UtcNow}"
            };

            dbContext.OutboxMessages.Add(message);
            await dbContext.SaveChangesAsync(stoppingToken); // ✅ Immediately triggers RabbitMQ publishing
        }
    }
}