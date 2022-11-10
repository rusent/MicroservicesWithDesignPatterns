using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared;
using Stock.API.Models;

namespace Stock.API.Consumers
{
    public class PaymentFailedEventConsumer : IConsumer<PaymentFailedEvent>
    {
        private readonly AppDbContext _dbContext;
        private ILogger<OrderCreatedEventConsumer> _logger;

        public PaymentFailedEventConsumer(AppDbContext dbContext, ILogger<OrderCreatedEventConsumer> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
        {
            foreach (var item in context.Message.OrderItems)
            {
                var stock = await _dbContext.Stocks.FirstOrDefaultAsync(x => x.ProductId == item.ProductId);

                if (stock != null)
                {
                    stock.Count += item.Count;
                    await _dbContext.SaveChangesAsync();
                }
            }

            _logger.LogInformation($"Stock was released for Order Id ({context.Message.OrderId})");
        }
    }
}
