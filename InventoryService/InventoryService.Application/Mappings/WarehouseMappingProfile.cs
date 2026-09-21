using AutoMapper;
using InventoryService.Application.Services.DTOs;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Entities.ValueObjects;

namespace InventoryService.Application.Mappings
{
    public class WarehouseMappingProfile : Profile
    {
        public WarehouseMappingProfile()
        {
            CreateMap<WorkingHours, WarehouseServiceDTOs.WorkingHoursDTO>().ReverseMap();
            CreateMap<Warehouse, WarehouseServiceDTOs.WarehouseDTO>();
        }
    }
}