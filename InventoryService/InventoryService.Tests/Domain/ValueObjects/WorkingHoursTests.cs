using InventoryService.Domain.Entities.ValueObjects;
using Xunit;

namespace InventoryService.Tests.Domain.ValueObjects
{
    public class WorkingHoursTests
    {
        [Theory]
        [InlineData(false, "09:00", "17:00", "08:59", false)]
        [InlineData(false, "09:00", "17:00", "09:00", true)]
        [InlineData(false, "09:00", "17:00", "12:00", true)]
        [InlineData(false, "09:00", "17:00", "16:59", true)]
        [InlineData(false, "09:00", "17:00", "17:00", false)]
        [InlineData(false, "09:00", "17:00", "17:01", false)]
        [InlineData(false, "22:00", "06:00", "22:00", true)]
        [InlineData(false, "22:00", "06:00", "23:00", true)]
        [InlineData(false, "22:00", "06:00", "02:00", true)]
        [InlineData(false, "22:00", "06:00", "05:59", true)]
        [InlineData(false, "22:00", "06:00", "06:00", false)]
        [InlineData(false, "22:00", "06:00", "12:00", false)]
        [InlineData(false, "00:00", "00:00", "00:00", true)]
        [InlineData(false, "00:00", "00:00", "13:37", true)]
        [InlineData(true, "09:00", "17:00", "12:00", false)]
        [InlineData(true, "00:00", "00:00", "00:00", false)]
        public void IsOpenAt_ReturnsExpected(bool isClosed, string opensAt, string closesAt, string time, bool expected)
        {
            var workingHours = WorkingHours.Create(
                DayOfWeek.Monday, isClosed, TimeOnly.Parse(opensAt), TimeOnly.Parse(closesAt));

            var result = workingHours.IsOpenAt(TimeOnly.Parse(time));

            Assert.Equal(expected, result);
        }
    }
}
