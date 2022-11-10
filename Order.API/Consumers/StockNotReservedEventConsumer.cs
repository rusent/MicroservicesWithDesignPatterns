using MassTransit;
using Order.API.Models;
using Shared;

namespace Order.API.Consumers
{
    public class StockNotReservedEventConsumer : IConsumer<StockNotReservedEvent>
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<PaymentFailedEventConsumer> _logger;

        public StockNotReservedEventConsumer(AppDbContext dbContext, ILogger<PaymentFailedEventConsumer> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }


        public async Task Consume(ConsumeContext<StockNotReservedEvent> context)
        {
            var order = await _dbContext.Orders.FindAsync(context.Message.OrderId);

            if (order != null)
            {
                order.Status = OrderStatus.Fail;
                order.FailMessage = context.Message.Message;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Order (Id={context.Message.OrderId}) status changed : {order.Status}");
            }
            else
            {
                _logger.LogInformation($"Order (Id={context.Message.OrderId}) not found");

            }
        }
    }
}
