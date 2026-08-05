using Xunit;

// Integration tests share process-global state (env vars used to configure each test class's
// WebApplicationFactory<Program> — see LeadRecoveryWebApplicationFactory) and each spins up its
// own Testcontainers MongoDB. Running collections in parallel would race on those env vars, so
// parallelization is disabled assembly-wide; the whole suite still runs in a few minutes locally.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
