using InventoryService.Domain.Entities.Base;

namespace InventoryService.Domain.Entities
{
    public class Supplier : BaseEntity
    {
        public string Name { get; private set; }
        public string? ContactEmail { get; private set; }
        public string? ContactPhone { get; private set; }

        private Supplier() { }

        public static Supplier Create(string name, string? contactEmail, string? contactPhone)
        {
            return new Supplier
            {
                Id = Guid.NewGuid(),
                Name = name,
                ContactEmail = contactEmail,
                ContactPhone = contactPhone
            };
        }

        public void Rename(string newName)
        {
            Name = newName;
        }

        public void UpdateContactEmail(string? contactEmail)
        {
            ContactEmail = contactEmail;
        }

        public void UpdateContactPhone(string? contactPhone)
        {
            ContactPhone = contactPhone;
        }
    }
}