using AutoMapper;
using InventoryService.Application.Services.DTOs;
using InventoryService.Domain.Entities;

namespace InventoryService.Application.Mappings
{
    public class LowStockAlertMappingProfile : Profile
    {
        public LowStockAlertMappingProfile()
        {
            CreateMap<LowStockAlert, LowStockAlertServiceDTOs.LowStockAlertDTO>();
        }
    }
}