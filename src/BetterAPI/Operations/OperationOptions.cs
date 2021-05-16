namespace BetterAPI.Operations
{
    public sealed class OperationOptions
    {
        /// <summary>
        /// Indicates the default amount of time an operation suggests clients retry. This value appears in the Retry-After
        /// response header. This value is used when an operation is not otherwise configured with a custom retry time, or
        /// has deterministic time completion.
        /// <seealso href="https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#1329-retry-after"/>
        /// </summary>
        public int? DefaultRetryAfterSeconds { get; set; } = 60;

        /// <summary>
        /// The amount of time to keep completed operation results until transitioning them to the tombstone state.
        /// A null value indicates operations are kept indefinitely and never transition to the tombstone state.
        /// <seealso href="https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#133-retention-policy-for-operation-results"/>
        /// </summary>
        public int? RetentionTimeSeconds { get; set; } = 1440;

        /// <summary>
        /// The amount of time to keep tombstone operation results until they are no longer available in queries.
        /// A null value indicates tombstones are kept indefinitely and are never deleted.
        /// <seealso href="https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#133-retention-policy-for-operation-results"/>
        /// </summary>
        public int? TombstoneTimeSeconds { get; set; } = 1440;

        /// <summary>
        /// Indicates whether expired tombstones are purged from the system of record.
        /// This requires that both <see cref="RetentionTimeSeconds"/> and <see cref="TombstoneTimeSeconds"/> have non-null values.
        /// <seealso href="https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#133-retention-policy-for-operation-results"/>
        /// </summary>
        public bool DeleteExpiredTombstones { get; set; } = true;

        /// <summary>
        ///     The number of threads available for performing tasks; default is 0.
        ///     A value of 0 defaults to the number of logical processors.
        /// </summary>
        public int Concurrency { get; set; } = 0;

        /// <summary>
        ///     The time to delay before checking for available tasks in the backing store. Default is 10 seconds.
        /// </summary>
        public int SleepIntervalSeconds { get; set; } = 10;
    }
}
