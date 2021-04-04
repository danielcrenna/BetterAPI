namespace BetterAPI.Operations
{
    public enum OperationState
    {
        NotStarted,
        Running,
        Succeeded,
        Failed,
        Cancelling,
        Cancelled,
        Aborting,
        Aborted,
        Tombstone,
        Deleting,
        Deleted
    }
}
