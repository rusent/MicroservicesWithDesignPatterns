using MassTransit;
using Order.API.Models;
using Shared;

namespace Order.API.Consumers
{
    public class PaymentCompletedEventConsumer : IConsumer<PaymentCompletedEvent>
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<PaymentCompletedEventConsumer> _logger;

        public PaymentCompletedEventConsumer(AppDbContext dbContext, ILogger<PaymentCompletedEventConsumer> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
        {
            var order = await _dbContext.Orders.FindAsync(context.Message.OrderId);

            if (order != null)
            {
                order.Status = OrderStatus.Complete;
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
