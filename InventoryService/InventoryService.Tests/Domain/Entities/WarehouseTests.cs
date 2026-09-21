using InventoryService.Domain.Entities;
using InventoryService.Domain.Entities.ValueObjects;
using Xunit;

namespace InventoryService.Tests.Domain.Entities
{
    public class WarehouseTests
    {
        private static Warehouse CreateWarehouse(
            WarehouseStatus status, params (DayOfWeek Day, bool IsClosed, TimeOnly Opens, TimeOnly Closes)[] overrides)
        {
            var overrideMap = overrides.ToDictionary(entry => entry.Day);

            var week = Enum.GetValues<DayOfWeek>()
                .Select(day => overrideMap.TryGetValue(day, out var entry)
                    ? WorkingHours.Create(day, entry.IsClosed, entry.Opens, entry.Closes)
                    : WorkingHours.Create(day, isClosed: true, TimeOnly.MinValue, TimeOnly.MinValue))
                .ToList();

            return new Warehouse(Guid.NewGuid(), "Test Warehouse", "Test Location", status, week);
        }

        [Theory]
        [InlineData(WarehouseStatus.Closed)]
        [InlineData(WarehouseStatus.UnderMaintenance)]
        public void IsOpenNow_StatusNotActive_ReturnsFalseEvenDuringConfiguredOpenHours(WarehouseStatus status)
        {
            var warehouse = CreateWarehouse(status,
                (DayOfWeek.Monday, false, TimeOnly.MinValue, TimeOnly.MinValue));

            var now = new DateTimeOffset(2023, 1, 2, 12, 0, 0, TimeSpan.Zero);

            Assert.False(warehouse.IsOpenNow(now));
        }

        [Fact]
        public void IsOpenNow_ActiveAndWithinTodaysWorkingHours_ReturnsTrue()
        {
            var warehouse = CreateWarehouse(WarehouseStatus.Active,
                (DayOfWeek.Monday, false, new TimeOnly(9, 0), new TimeOnly(17, 0)));

            var now = new DateTimeOffset(2023, 1, 2, 12, 0, 0, TimeSpan.Zero);

            Assert.True(warehouse.IsOpenNow(now));
        }

        [Fact]
        public void IsOpenNow_ConvertsNonUtcOffsetToUtc_UsesUtcDayOfWeekNotLocalDay()
        {
            // Local wall-clock (2023-01-02 01:00 +03:00) is a Monday; converted to UTC it is
            // 2023-01-01 22:00, a Sunday. Sunday is configured closed, Monday is open all day —
            // this only passes if the UTC day (Sunday) is used, not the offset's local day (Monday).
            var warehouse = CreateWarehouse(WarehouseStatus.Active,
                (DayOfWeek.Sunday, true, TimeOnly.MinValue, TimeOnly.MinValue),
                (DayOfWeek.Monday, false, TimeOnly.MinValue, TimeOnly.MinValue));

            var now = new DateTimeOffset(2023, 1, 2, 1, 0, 0, TimeSpan.FromHours(3));

            Assert.False(warehouse.IsOpenNow(now));
        }
    }
}
