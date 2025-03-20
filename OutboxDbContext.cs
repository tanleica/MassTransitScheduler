using MassTransit;
using Microsoft.EntityFrameworkCore;

public class OutboxDbContext : DbContext
{
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    private readonly IBus _bus;

    public OutboxDbContext(DbContextOptions<OutboxDbContext> options, IBus bus) : base(options)
    {
        _bus = bus;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var newMessages = ChangeTracker.Entries<OutboxMessage>()
                                       .Where(e => e.State == EntityState.Added)
                                       .Select(e => e.Entity)
                                       .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        // 🔹 Immediately publish messages after saving to DB
        foreach (var message in newMessages)
        {
            await _bus.Publish(message, cancellationToken);
            Console.WriteLine($" [✔] Message published immediately: {message.Message}");
        }

        return result;
    }
}
