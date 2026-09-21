using AutoMapper;
using InventoryService.Application.Services.DTOs;
using InventoryService.Domain.Entities;

namespace InventoryService.Application.Mappings
{
    public class ProductMappingProfile : Profile
    {
        public ProductMappingProfile()
        {
            CreateMap<Product, ProductServiceDTOs.ProductDTO>();
        }
    }
}