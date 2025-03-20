using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MassTransit;

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
                cfg.Host("rabbitmq", h =>
                {
                    h.Username("admin");
                    h.Password("A123231312a@");
                });
            });
        });

        var serviceProvider = services.BuildServiceProvider();

        var dbContext = serviceProvider.GetRequiredService<OutboxDbContext>();

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