using Quartz;
using Quartz.Impl;
using TimeZoneConverter;
using System.Globalization;
using RabbitMQ.Client;
using System.Text;

class Program
{
    static async Task Main()
    {
        // ✅ Create a Quartz Scheduler
        IScheduler scheduler = await StdSchedulerFactory.GetDefaultScheduler();
        await scheduler.Start();

        // ✅ Get the Desired Time Zone (e.g., Vietnam)
        TimeZoneInfo targetTimeZone = TZConvert.GetTimeZoneInfo("Asia/Ho_Chi_Minh");

        // ✅ Define the Daily Job (7:00 AM)
        IJobDetail dailyJob = JobBuilder.Create<SendMessageJob>()
            .WithIdentity("DailyMessageJob", "DailyTasks")
            .Build();

        ITrigger dailyTrigger = TriggerBuilder.Create()
            .WithIdentity("DailyTrigger", "DailyTasks")
            .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(7, 0)
                .InTimeZone(targetTimeZone)) // ✅ Ensure it runs in Vietnam Time
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(dailyJob, dailyTrigger);
        Console.WriteLine($" [✔] Daily job scheduled to run at: {dailyTrigger.GetNextFireTimeUtc()} UTC");

        // ✅ Schedule a temporary job (runs after 2 minutes)
        await ScheduleTemporaryJob(scheduler, 2);

        Console.WriteLine(" [*] Scheduler started. Waiting for execution...");
        await Task.Delay(-1); // Keep application running
    }

    static async Task ScheduleTemporaryJob(IScheduler scheduler, int minutesDelay)
    {
        IJobDetail tempJob = JobBuilder.Create<SendMessageJob>()
            .WithIdentity("TempMessageJob", "TemporaryTasks")
            .Build();

        ITrigger tempTrigger = TriggerBuilder.Create()
            .WithIdentity("TempTrigger", "TemporaryTasks")
            .StartAt(DateTimeOffset.UtcNow.AddMinutes(minutesDelay)) // 🔹 Runs after 2 minutes
            .Build();

        await scheduler.ScheduleJob(tempJob, tempTrigger);
        Console.WriteLine($" [✔] Temporary job scheduled to run at: {tempTrigger.GetNextFireTimeUtc()} UTC");
    }
}

// ✅ Define the Scheduled Job
public class SendMessageJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        // ✅ Ensure time is in Vietnam time
        TimeZoneInfo targetTimeZone = TZConvert.GetTimeZoneInfo("Asia/Ho_Chi_Minh");
        DateTime localTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, targetTimeZone);

        // ✅ Format Date in Vietnamese
        CultureInfo culture = new("vi-VN");
        string message = $"Today is {localTime.ToString("dddd, MMMM dd, yyyy", culture)}";

        Console.WriteLine($" [✔] Sending: {message}");
        await SendMessageToRabbitMQ(message);
    }

    static async Task SendMessageToRabbitMQ(string message)
    {
        await Task.Run(() =>
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = "159.223.59.17", // Your RabbitMQ server address
                    Port = 5672, // Default RabbitMQ port
                    UserName = "admin", // Your RabbitMQ username
                    Password = "A123231312a@" // Your RabbitMQ password
                };

                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                string queueName = "test_queue";

                // ✅ Declare the queue to ensure it exists
                channel.QueueDeclare(queue: queueName,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var body = Encoding.UTF8.GetBytes(message);

                // ✅ Publish the message
                channel.BasicPublish(exchange: "",
                                     routingKey: queueName,
                                     basicProperties: null,
                                     body: body);

                Console.WriteLine($" [✔] Message sent to RabbitMQ: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" [!] RabbitMQ Send Error: {ex.Message}");
            }
        });
    }
}