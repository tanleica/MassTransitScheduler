using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MassTransit;

namespace MassTransitScheduler;
class Program
{
    static async Task Main()
    {
        Console.WriteLine(" [*] MassTransitScheduler Started...");

        var services = new ServiceCollection();
        services.AddDbContext<OutboxDbContext>(options =>
            options.UseSqlServer("Server=159.223.59.17,1433;Database=MassTransitDB;User Id=sa;Password=A123231312a@;TrustServerCertificate=True;"));

        services.AddMassTransit(cfg =>
        {
            cfg.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host("159.223.59.17", h =>
                {
                    h.Username("admin");
                    h.Password("A123231312a@");
                });
            });
        });

        var serviceProvider = services.BuildServiceProvider();

        var dbContext = serviceProvider.GetRequiredService<OutboxDbContext>();

        #region Test after 2 minutes
        await Task.Delay(TimeSpan.FromMinutes(2)); // 🔹 Test message

        var testMessage = new OutboxMessage
        {
            UserId = "b2e05ec4-6022-4f35-baea-ceb7fa2ee9dd",
            Message = $"Mass test message at {DateTime.UtcNow}"
        };

        dbContext.OutboxMessages.Add(testMessage);
        await dbContext.SaveChangesAsync(); // ✅ Immediately triggers RabbitMQ publishing
        Console.WriteLine(" [*] MassTransitScheduler saved a test record...");
        #endregion Test after 2 minutes

        while (true)
        {
            await Task.Delay(TimeSpan.FromHours(1)); // 🔹 Runs every hour

            var message = new OutboxMessage
            {
                UserId = "b2e05ec4-6022-4f35-baea-ceb7fa2ee9dd",
                Message = $"Mass at {DateTime.UtcNow}"
            };

            dbContext.OutboxMessages.Add(message);
            await dbContext.SaveChangesAsync(); // ✅ Immediately triggers RabbitMQ publishing
        }
    }
}