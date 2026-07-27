// FieldProtection holds process-wide static state and the integration tests share a
// single database, so run tests sequentially to keep them deterministic.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
