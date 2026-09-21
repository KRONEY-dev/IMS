namespace Shared.Kernel.Scheduling
{
    public interface ICronJobSettings
    {
        string CronExpression { get; }
    }
}
