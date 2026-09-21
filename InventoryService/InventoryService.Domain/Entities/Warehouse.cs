using InventoryService.Domain.Entities.Base;
using InventoryService.Domain.Entities.ValueObjects;
using System.Diagnostics.CodeAnalysis;
using static InventoryService.Domain.Exceptions.GeneralExceptions;

namespace InventoryService.Domain.Entities
{
    public enum WarehouseStatus
    {
        Active,
        Closed,
        UnderMaintenance
    }

    public class Warehouse : BaseEntity
    {
        public string Name { get; private set; }
        public string Location { get; private set; }

        public WarehouseStatus Status { get; private set; }

        public IReadOnlyList<WorkingHours> WorkingHours { get; private set; }

        private Warehouse() { }

        [SetsRequiredMembers]
        public Warehouse(Guid id, string name, string location, WarehouseStatus status, IReadOnlyList<WorkingHours> workingHours)
        {
            EnsureCoversEveryDayOfWeek(workingHours);

            Id = id;
            Name = name;
            Location = location;
            Status = status;
            WorkingHours = workingHours;
        }

        public static Warehouse Create(string name, string location, IReadOnlyList<WorkingHours> workingHours)
        {
            return new Warehouse(Guid.NewGuid(), name, location, WarehouseStatus.Active, workingHours);
        }

        public void ChangeStatus(WarehouseStatus newStatus)
        {
            Status = newStatus;
        }

        public void UpdateWorkingHours(IReadOnlyList<WorkingHours> workingHours)
        {
            EnsureCoversEveryDayOfWeek(workingHours);

            WorkingHours = workingHours;
        }

        public bool IsOpenNow(DateTimeOffset now)
        {
            if (Status != WarehouseStatus.Active)
            {
                return false;
            }

            var utcNow = now.UtcDateTime;
            var utcTimeOfDay = TimeOnly.FromDateTime(utcNow);
            var todaysHours = WorkingHours.Single(entry => entry.DayOfWeek == utcNow.DayOfWeek);

            return todaysHours.IsOpenAt(utcTimeOfDay);
        }

        private static void EnsureCoversEveryDayOfWeek(IReadOnlyList<WorkingHours> workingHours)
        {
            if (workingHours.Select(entry => entry.DayOfWeek).Distinct().Count() != 7)
            {
                throw new InvalidWorkingHoursException();
            }
        }
    }
}