namespace InventoryService.Application.Services.DTOs
{
    public static class SupplierServiceDTOs
    {
        public record CreateSupplierRequestDTO(string Name, string? ContactEmail, string? ContactPhone);

        public record GetAllSuppliersRequestDTO();
        public record GetAllSuppliersResponseDTO(IReadOnlyList<SupplierDTO> Suppliers);

        public record GetSupplierByIdRequestDTO(Guid SupplierId);

        public record UpdateSupplierRequestDTO(Guid SupplierId, string Name, string? ContactEmail, string? ContactPhone);
        public record UpdateSupplierResponseDTO();

        public record DeleteSupplierRequestDTO(Guid SupplierId);
        public record DeleteSupplierResponseDTO();

        public record SupplierDTO(Guid Id, string Name, string? ContactEmail, string? ContactPhone);
    }
}