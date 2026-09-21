using AutoMapper;
using InventoryService.Application.Services.DTOs;
using InventoryService.Domain.Entities;

namespace InventoryService.Application.Mappings
{
    public class StockMappingProfile : Profile
    {
        public StockMappingProfile()
        {
            CreateMap<StockThreshold, StockServiceDTOs.StockThresholdDTO>();
            CreateMap<StockItem, StockServiceDTOs.StockItemDTO>();
            CreateMap<StockMovement, StockServiceDTOs.StockMovementDTO>();
        }
    }
}