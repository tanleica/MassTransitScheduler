using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace MassTransitScheduler;
public class OutboxDbContext(DbContextOptions<OutboxDbContext> options, IBus bus) : DbContext(options)
{
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    private readonly IBus _bus = bus;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var newMessages = ChangeTracker.Entries<OutboxMessage>()
                                           .Where(e => e.State == EntityState.Added)
                                           .Select(e => e.Entity)
                                           .ToList();

            var result = await base.SaveChangesAsync(cancellationToken);

            var sendEndpoint = await _bus.GetSendEndpoint(new Uri("queue:outbox-queue"));
            foreach (var message in newMessages)
            {
                await sendEndpoint.Send(message, cancellationToken);
                Console.WriteLine($" [✔] Message sent to 'outbox-queue': {message.Message}");
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($" [!] Error in SaveChangesAsync: {ex.Message}");
            Console.WriteLine($" [!] StackTrace: {ex.StackTrace}");
            throw;
        }
    }

}