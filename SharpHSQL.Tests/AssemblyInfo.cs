using Xunit;

// Disable parallel test execution to prevent database state conflicts between tests
// All test classes share the same in-memory database instance (".") via DatabaseController cache
[assembly: CollectionBehavior(DisableTestParallelization = true)]
