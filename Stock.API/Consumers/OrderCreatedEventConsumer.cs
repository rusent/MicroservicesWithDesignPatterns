using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared;
using Stock.API.Models;

namespace Stock.API.Consumers
{
    public class OrderCreatedEventConsumer : IConsumer<OrderCreatedEvent>
    {
        private readonly AppDbContext _dbContext;
        private ILogger<OrderCreatedEventConsumer> _logger;
        private readonly ISendEndpointProvider _sendEndpointProvider;
        private readonly IPublishEndpoint _publishEndpoint;

        public OrderCreatedEventConsumer(AppDbContext dbContext, ILogger<OrderCreatedEventConsumer> logger,
            ISendEndpointProvider sendEndpointProvider,  IPublishEndpoint publishEndpoint)
        {
            _dbContext = dbContext;
            _logger = logger;
            _sendEndpointProvider = sendEndpointProvider;
            _publishEndpoint = publishEndpoint;
        }

        public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
        {
            var stockResult = new List<bool>();

            foreach (var item in context.Message.OrderItems)
            {
                stockResult.Add(await _dbContext.Stocks.AnyAsync(x => x.ProductId == item.ProductId && x.Count > item.Count));
            }

            if (stockResult.All(x => x.Equals(true)))
            {
                foreach (var item in context.Message.OrderItems)
                {
                    var stock = await _dbContext.Stocks.FirstOrDefaultAsync(x => x.ProductId == item.ProductId);

                    if (stock != null)
                    {
                        stock.Count -= item.Count;
                    }

                    await _dbContext.SaveChangesAsync();
                }

                _logger.LogInformation($"Stock was reserved for Buyer Id :{ context.Message.BuyerId}");

                var sendEndpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{RabbitMQSettingsConst.StockReservedEventQueueName}"));

                StockReservedEvent stockReservedEvent = new StockReservedEvent()
                {
                    Payment = context.Message.Payment,
                    BuyerId = context.Message.BuyerId,
                    OrderId = context.Message.OrderId,
                    OrderItems = context.Message.OrderItems
                };

                await sendEndpoint.Send(stockReservedEvent);
            
            }
            else
            {
                await _publishEndpoint.Publish(new StockNotReservedEvent()
                {
                    OrderId = context.Message.OrderId,
                    Message = "Not enough stock"
                });

                _logger.LogInformation($"Not enough stock for Buyer Id :{context.Message.BuyerId}");
            }
        }
    }
}
