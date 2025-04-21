using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.ServiceBus;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Data;
using Models;
using Microsoft.EntityFrameworkCore;

namespace Workers;

public class OrderProcessorWorker : BackgroundService
{
    private readonly ILogger<OrderProcessorWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly OrderContext _context;
    private readonly IQueueClient _queueClient;

    public OrderProcessorWorker(ILogger<OrderProcessorWorker> logger, IConfiguration configuration, OrderContext context)
    {
        _logger = logger;
        _configuration = configuration;
        _context = context;
        
        var connectionString = _configuration.GetValue<string>("AzureBusConnectionString");
        var queueName = _configuration.GetValue<string>("QueueName");
        
        _queueClient = new QueueClient(connectionString, queueName);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker de processamento de pedidos iniciado em: {time}", DateTimeOffset.Now);

        var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
        {
            MaxConcurrentCalls = 1,
            AutoComplete = false
        };

        _queueClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        await _queueClient.CloseAsync();
    }

    private async Task ProcessMessagesAsync(Message message, CancellationToken token)
    {
        try
        {
            var messageBody = Encoding.UTF8.GetString(message.Body);
            var order = JsonSerializer.Deserialize<Order>(messageBody);

            if (order != null)
            {
                _logger.LogInformation("Processando pedido: {OrderId}", order.Id);

                // Atualiza o status para "Processando"
                var orderToUpdate = await _context.Orders.FindAsync(order.Id);
                if (orderToUpdate != null)
                {
                    orderToUpdate.Status = "Processado";
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Status do pedido {OrderId} atualizado para Processando", order.Id);

                    // Simula processamento de 5 segundos
                    await Task.Delay(5000, token);

                    // Atualiza o status para "Finalizado"
                    orderToUpdate.Status = "Finalizado";
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Status do pedido {OrderId} atualizado para Finalizado", order.Id);
                }

                await _queueClient.CompleteAsync(message.SystemProperties.LockToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem");
            await _queueClient.AbandonAsync(message.SystemProperties.LockToken);
        }
    }

    private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
    {
        _logger.LogError(exceptionReceivedEventArgs.Exception, "Erro ao receber mensagem");
        return Task.CompletedTask;
    }
} 