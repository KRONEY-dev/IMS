using AutoMapper;
using InventoryService.Application.Services.DTOs;
using InventoryService.Domain.Entities;

namespace InventoryService.Application.Mappings
{
    public class StockTransferMappingProfile : Profile
    {
        public StockTransferMappingProfile()
        {
            CreateMap<StockTransfer, StockTransferServiceDTOs.StockTransferDTO>();
        }
    }
}