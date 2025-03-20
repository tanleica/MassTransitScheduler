using MassTransit;

class Program
{
    static async Task Main()
    {
        Console.WriteLine(" [*] MassTransitScheduler Started...");

        var busControl = Bus.Factory.CreateUsingRabbitMq(cfg =>
        {
            cfg.Host("rabbitmq", h =>
            {
                h.Username("admin");
                h.Password("A123231312a@");
            });
        });

        await busControl.StartAsync();
        Console.WriteLine(" [✔] Connected to RabbitMQ");

        while (true)
        {
            await Task.Delay(TimeSpan.FromHours(1)); // 🔹 Runs every hour

            using var dbContext = new OutboxDbContext();
            var message = new OutboxMessage
            {
                UserId = "b2e05ec4-6022-4f35-baea-ceb7fa2ee9dd",
                Message = $"Scheduled Notification at {DateTime.UtcNow}"
            };

            dbContext.OutboxMessages.Add(message);
            await dbContext.SaveChangesAsync(); // ✅ Immediately triggers RabbitMQ publishing
        }
    }
}