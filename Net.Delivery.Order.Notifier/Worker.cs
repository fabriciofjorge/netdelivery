using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Net.Delivery.Order.Domain.Services;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Net.Delivery.Order.Notifier;

public class Worker(ILogger<Worker> logger, IConfiguration configuration) : BackgroundService
{
    private readonly string _orderTopicName = configuration["OrderSettings:OrderTopicName"];
    private readonly string _kafkaBootstrapServers = configuration["OrderSettings:KafkaBootstrapServer"];
    private readonly string _notifierConsumeGroupName = configuration["OrderSettings:NotifierConsumeGroupName"];

    /// <summary>
    /// Lopping process
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        INotifierService notifierService = new NotifierService();

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Order notifier running at: {time}", DateTimeOffset.Now);

            var config = new ConsumerConfig
            {
                BootstrapServers = _kafkaBootstrapServers,
                GroupId = _notifierConsumeGroupName,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe(_orderTopicName);

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => {
                e.Cancel = true;
                cts.Cancel();
            };

            try
            {
                while (true)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(cts.Token);

                        var order = JsonSerializer.Deserialize<Domain.Entities.Order>(consumeResult.Message.Value);

                        notifierService.Notify(order);

                        Console.WriteLine($"Mensagem recebida: {consumeResult.Message.Value}");
                    }
                    catch (ConsumeException e)
                    {
                        Console.WriteLine($"Erro ao consumir a mensagem: {e.Error.Reason}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                consumer.Close();
            }
        }

        return Task.CompletedTask;
    }
}
