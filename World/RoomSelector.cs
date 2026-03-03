using System.Collections.Generic;
using Godot;

/// <summary>
/// Handles weighted random room selection with guaranteed spawns and max-count limits.
/// </summary>
public class RoomSelector
{
    private readonly PackedScene[] _scenes;
    private readonly int[] _weights;
    private readonly int[] _maxCounts;
    private readonly int[] _guaranteed;
    private readonly Dictionary<int, int> _spawnCounts = new();
    private readonly RandomNumberGenerator _rng = new();

    public RoomSelector(PackedScene[] scenes, int[] weights, int[] maxCounts, int[] guaranteed)
    {
        _scenes = scenes;
        _weights = weights;
        _maxCounts = maxCounts;
        _guaranteed = guaranteed;
        _rng.Randomize();

        for (int i = 0; i < scenes.Length; i++)
            _spawnCounts[i] = 0;
    }

    /// <summary>
    /// Picks a room index (or uses forcedIndex), increments spawn count, and returns the index.
    /// Returns -1 if no rooms are available.
    /// </summary>
    public int SelectRoom(int forcedIndex = -1)
    {
        if (_scenes.Length == 0) return -1;

        int index = (forcedIndex >= 0) ? forcedIndex : ChooseRoom();
        if (index < 0) return -1;

        _spawnCounts[index]++;
        GD.Print($"Spawned room {index} (count: {GetSpawnCount(index)}/{GetMaxCount(index)})");
        return index;
    }

    public PackedScene GetScene(int index) => _scenes[index];
    public int GetSpawnCount(int index) => _spawnCounts.GetValueOrDefault(index, 0);
    public int GetMaxCount(int index) => (index < _maxCounts.Length) ? _maxCounts[index] : -1;

    private int ChooseRoom()
    {
        // Guaranteed rooms that haven't spawned yet take priority
        for (int i = 0; i < _scenes.Length; i++)
        {
            if (_scenes[i] == null) continue;
            bool isGuaranteed = i < _guaranteed.Length && _guaranteed[i] == 1;
            if (isGuaranteed && _spawnCounts[i] == 0)
            {
                GD.Print($"Spawning guaranteed room {i}");
                return i;
            }
        }

        // Otherwise use weighted random selection
        return PickWeightedRandom();
    }

    private int PickWeightedRandom()
    {
        var available = new List<(int index, int weight)>();

        for (int i = 0; i < _scenes.Length; i++)
        {
            if (_scenes[i] == null) continue;
            int weight = (i < _weights.Length) ? _weights[i] : 1;
            int max = (i < _maxCounts.Length) ? _maxCounts[i] : -1;

            if (max == -1 || _spawnCounts[i] < max)
                available.Add((i, weight));
        }

        if (available.Count == 0) return -1;

        int total = 0;
        foreach (var (_, w) in available) total += w;

        int roll = _rng.RandiRange(0, total - 1);
        int cumulative = 0;
        foreach (var (index, w) in available)
        {
            cumulative += w;
            if (roll < cumulative) return index;
        }

        return available[0].index;
    }
}
