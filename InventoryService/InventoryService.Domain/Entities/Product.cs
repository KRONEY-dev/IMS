using InventoryService.Domain.Entities.Base;

namespace InventoryService.Domain.Entities
{
    public class Product : BaseEntity
    {
        public string Name { get; private set; }
        public string ProductCategory { get; private set; }

        public string? PhotoUrl { get; private set; }

        private Product() { }

        public static Product Create(string name, string productCategory, string? photoUrl)
        {
            return new Product
            {
                Id = Guid.NewGuid(),
                Name = name,
                ProductCategory = productCategory,
                PhotoUrl = photoUrl
            };
        }

        public void Rename(string newName)
        {
            Name = newName;
        }

        public void ChangeCategory(string newCategory)
        {
            ProductCategory = newCategory;
        }

        public void SetPhotoUrl(string? photoUrl)
        {
            PhotoUrl = photoUrl;
        }
    }
}