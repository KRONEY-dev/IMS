using InventoryService.Domain.Entities.Base;
using System.Linq.Expressions;
using System.Text.Json;

namespace InventoryService.Domain.Entities
{
    public class OutboxMessage : BaseEntity
    {
        public required string EventType { get; init; }
        public required string Payload { get; init; }

        public required DateTime OccurredAt { get; init; }
        public DateTime? ProcessedAt { get; init; }

        private OutboxMessage() { }

        public static OutboxMessage Create(string eventType, string payload)
        {
            return new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = eventType,
                Payload = payload,
                OccurredAt = DateTime.UtcNow,
                ProcessedAt = null
            };
        }

        public static OutboxMessage CreateFor<TEvent>(TEvent eventData)
        {
            return Create(typeof(TEvent).Name, JsonSerializer.Serialize(eventData));
        }

        public static Expression<Func<OutboxMessage, bool>> IsUnprocessed()
        {
            return message => message.ProcessedAt == null;
        }

        public static Expression<Func<OutboxMessage, bool>> IsUnprocessed(Guid id)
        {
            return message => message.Id == id && message.ProcessedAt == null;
        }
    }
}