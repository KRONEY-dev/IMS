using AutoMapper;
using InventoryService.Application.Services.DTOs;
using InventoryService.Domain.Entities;

namespace InventoryService.Application.Mappings
{
    public class SupplierOrderMappingProfile : Profile
    {
        public SupplierOrderMappingProfile()
        {
            CreateMap<SupplierOrderItem, SupplierOrderServiceDTOs.SupplierOrderItemDTO>();
        }
    }
}