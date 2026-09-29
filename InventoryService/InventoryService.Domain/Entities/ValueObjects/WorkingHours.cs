namespace InventoryService.Domain.Entities.ValueObjects
{
    public class WorkingHours
    {
        public DayOfWeek DayOfWeek { get; private set; }
        public bool IsClosed { get; private set; }
        public TimeOnly OpensAt { get; private set; }
        public TimeOnly ClosesAt { get; private set; }

        private WorkingHours() { }

        public WorkingHours(DayOfWeek dayOfWeek, bool isClosed, TimeOnly opensAt, TimeOnly closesAt)
        {
            DayOfWeek = dayOfWeek;
            IsClosed = isClosed;
            OpensAt = opensAt;
            ClosesAt = closesAt;
        }

        public static WorkingHours Create(DayOfWeek dayOfWeek, bool isClosed, TimeOnly opensAt, TimeOnly closesAt)
        {
            return new WorkingHours(dayOfWeek, isClosed, opensAt, closesAt);
        }

        public bool CoversStartingToday(TimeOnly time)
        {
            if (IsClosed)
            {
                return false;
            }

            if (OpensAt == ClosesAt)
            {
                return true;
            }

            return OpensAt < ClosesAt
                ? time >= OpensAt && time < ClosesAt
                : time >= OpensAt;
        }

        public bool CoversSpilloverFromYesterday(TimeOnly time)
        {
            return !IsClosed && OpensAt > ClosesAt && time < ClosesAt;
        }
    }
}