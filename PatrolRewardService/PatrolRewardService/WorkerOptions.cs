namespace PatrolRewardService;

public class WorkerOptions
{
    public const string WorkerConfig = "WorkerConfig";

    public int StageInterval { get; set; }

    public int ResultInterval { get; set; }

    public int StageTxCapacity { get; set; } = 100;
}
