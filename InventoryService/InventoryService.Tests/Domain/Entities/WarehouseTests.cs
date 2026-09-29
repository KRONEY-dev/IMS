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
            var warehouse = CreateWarehouse(WarehouseStatus.Active,
                (DayOfWeek.Sunday, true, TimeOnly.MinValue, TimeOnly.MinValue),
                (DayOfWeek.Monday, false, TimeOnly.MinValue, TimeOnly.MinValue));

            var now = new DateTimeOffset(2023, 1, 2, 1, 0, 0, TimeSpan.FromHours(3));

            Assert.False(warehouse.IsOpenNow(now));
        }

        [Fact]
        public void IsOpenNow_OvernightShiftSpillsIntoNextDayThatIsOtherwiseClosed_ReturnsTrue()
        {
            var warehouse = CreateWarehouse(WarehouseStatus.Active,
                (DayOfWeek.Saturday, false, new TimeOnly(22, 0), new TimeOnly(6, 0)),
                (DayOfWeek.Sunday, true, TimeOnly.MinValue, TimeOnly.MinValue));

            var now = new DateTimeOffset(2023, 1, 1, 2, 0, 0, TimeSpan.Zero);

            Assert.True(warehouse.IsOpenNow(now));
        }

        [Fact]
        public void IsOpenNow_TodaysOvernightShiftHasNotStartedYet_DoesNotBorrowTomorrowsHours()
        {
            var warehouse = CreateWarehouse(WarehouseStatus.Active,
                (DayOfWeek.Saturday, true, TimeOnly.MinValue, TimeOnly.MinValue),
                (DayOfWeek.Sunday, false, new TimeOnly(22, 0), new TimeOnly(6, 0)));

            var now = new DateTimeOffset(2023, 1, 1, 2, 0, 0, TimeSpan.Zero);

            Assert.False(warehouse.IsOpenNow(now));
        }
    }
}