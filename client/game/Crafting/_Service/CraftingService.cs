#nullable enable

namespace Nomad.Game.Crafting;

using System;
using System.Collections.Generic;
using SpacetimeDB.Types;
using StdbCraftingJob = SpacetimeDB.Types.CraftingJob;

// Plain-C# view of the CraftingJobs table: per-bench active job (Progress read
// straight off the row) plus queued count. Connected mode mirrors the server's
// rows; test mode (no connection) is seeded and ticked directly so pure
// harnesses drive the same modal + ring.
public class CraftingService
{
    private const int DefaultBenchInputSlots = 4;
    private const int DefaultBenchOutputSlots = 4;

    private readonly Dictionary<int, CraftingJobEntry> _jobs = [];
    private int _benchInputSlots = DefaultBenchInputSlots;
    private int _benchOutputSlots = DefaultBenchOutputSlots;
    private DbConnection? _conn;
    private int _nextTestJobId = 1;

    public event Action? Changed;

    // Test-mode completion hook — the harness deposits the output item into the
    // bench output zone via InventoryService (the HarvestService.Harvested
    // pattern).
    public event Action<int, string>? JobCompleted;

    // Test-mode queue mirror — the harness consumes ingredients and seeds a job
    // (the InventoryService.TestLoadRequested pattern).
    public event Action<int, string>? TestQueueRequested;

    public int BenchInputSlots => _benchInputSlots;

    public int BenchOutputSlots => _benchOutputSlots;

    public void BindConnection(DbConnection conn)
    {
        _conn = conn;

        if (conn.Db.CraftingConfigs.Id.Find(0) is { } config)
        {
            _benchInputSlots = config.BenchInputSlots;
            _benchOutputSlots = config.BenchOutputSlots;
        }

        foreach (var job in conn.Db.CraftingJobs.Iter())
            Apply(job);

        conn.Db.CraftingJobs.OnInsert += OnJobInserted;
        conn.Db.CraftingJobs.OnUpdate += OnJobUpdated;
        conn.Db.CraftingJobs.OnDelete += OnJobDeleted;

        Changed?.Invoke();
    }

    // The active job at a bench (CompletesAt set), or null if idle.
    public CraftingJobEntry? ActiveJobAt(int roomSlot)
    {
        foreach (var entry in _jobs.Values)
        {
            if (entry.RoomSlot == roomSlot && entry.IsActive)
                return entry;
        }
        return null;
    }

    public int QueuedCountAt(int roomSlot)
    {
        var count = 0;
        foreach (var entry in _jobs.Values)
        {
            if (entry.RoomSlot == roomSlot && !entry.IsActive)
                count++;
        }
        return count;
    }

    public void RequestQueueCraft(int roomSlot, string recipeId)
    {
        if (_conn is { } conn)
        {
            if (Enum.TryParse<RecipeId>(recipeId, out var parsed))
                conn.Reducers.QueueCraft(roomSlot, parsed);
            return;
        }

        TestQueueRequested?.Invoke(roomSlot, recipeId);
    }

    public void Unbind()
    {
        if (_conn is null)
            return;

        _conn.Db.CraftingJobs.OnInsert -= OnJobInserted;
        _conn.Db.CraftingJobs.OnUpdate -= OnJobUpdated;
        _conn.Db.CraftingJobs.OnDelete -= OnJobDeleted;
        _conn = null;
    }

    // ---- Test-mode mirror (no connection) ----

    public void ClearTestJobs()
    {
        _jobs.Clear();
        Changed?.Invoke();
    }

    public int SeedTestJob(int roomSlot, string recipeId, bool active)
    {
        var jobId = _nextTestJobId++;
        _jobs[jobId] = new CraftingJobEntry(jobId, roomSlot, recipeId, 0f, active);
        Changed?.Invoke();
        return jobId;
    }

    // Drives test-mode active jobs forward; the harness calls this each physics
    // frame. On completion it signals the harness to deposit the output, removes
    // the job, and activates the next queued job at that bench.
    public void AdvanceTestActiveJobs(float progressDelta)
    {
        if (_conn is not null)
            return;

        var completed = new List<CraftingJobEntry>();
        var advanced = false;
        foreach (var (jobId, entry) in _jobs)
        {
            if (!entry.IsActive)
                continue;

            var progress = entry.Progress + progressDelta;
            if (progress < 1f)
            {
                _jobs[jobId] = entry with { Progress = progress };
                advanced = true;
                continue;
            }

            completed.Add(entry);
        }

        foreach (var entry in completed)
        {
            _jobs.Remove(entry.JobId);
            JobCompleted?.Invoke(entry.RoomSlot, entry.RecipeId);
            ActivateNextTestQueued(entry.RoomSlot);
        }

        // Signal every frame progress moves so the ring animates and consumers
        // re-sync (HarvestService.AdvanceTestHarvest pattern).
        if (advanced || completed.Count > 0)
            Changed?.Invoke();
    }

    private void ActivateNextTestQueued(int roomSlot)
    {
        CraftingJobEntry? next = null;
        foreach (var entry in _jobs.Values)
        {
            if (entry.RoomSlot != roomSlot || entry.IsActive)
                continue;

            if (next is null || entry.JobId < next.JobId)
                next = entry;
        }

        if (next is { } chosen)
            _jobs[chosen.JobId] = chosen with { IsActive = true, Progress = 0f };
    }

    private void Apply(StdbCraftingJob job)
    {
        _jobs[job.JobId] = new CraftingJobEntry(
            job.JobId,
            job.RoomSlotIndex,
            job.RecipeId.ToString(),
            job.Progress,
            job.CompletesAt is not null
        );
    }

    private void OnJobDeleted(EventContext ctx, StdbCraftingJob job)
    {
        _jobs.Remove(job.JobId);
        Changed?.Invoke();
    }

    private void OnJobInserted(EventContext ctx, StdbCraftingJob job)
    {
        Apply(job);
        Changed?.Invoke();
    }

    private void OnJobUpdated(EventContext ctx, StdbCraftingJob oldJob, StdbCraftingJob newJob)
    {
        Apply(newJob);
        Changed?.Invoke();
    }
}

public record CraftingJobEntry(
    int JobId,
    int RoomSlot,
    string RecipeId,
    float Progress,
    bool IsActive
);
