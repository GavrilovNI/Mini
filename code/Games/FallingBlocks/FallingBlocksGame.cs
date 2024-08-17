using Sandbox;
using Sandbox.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mini.Games.FallingBlocks;

public sealed class FallingBlocksGame : MiniGame
{
    [Property]
    public Vector2Int Size { get; set; } = 8;

    [Property]
    public Vector3 BlockSize { get; set; } = new Vector3(3f, 3f, 1f);

    [Property]
    public GameObject FallingBlockPrefab { get; set; } = null!;

    [Property]
    public float DifficultyIncreaseTime = 60f;

    [Property]
    public float SpawningHeight { get; set; } = 1250f;

    [Property]
    public Curve SpawningSpeedCurve { get; set; }

    public float SpawningSpeed => SpawningSpeedCurve.Evaluate(TimeSinceStatusChanged / DifficultyIncreaseTime);

    [Property]
    public Curve BlocksSpeedCurve { get; set; }

    public float BlocksSpeed => BlocksSpeedCurve.Evaluate(TimeSinceStatusChanged / DifficultyIncreaseTime);

    [Property]
    public List<Color> BlockColors { get; set; } = new();


    private readonly Dictionary<Vector2Int, int> _groundedBlocksCount = new();
    private readonly Dictionary<Vector2Int, FallingBlock> _notGroundedBlocks = new();

    private TimeSince _timeSinceBlockSpawned;

    private Vector2Int[] _indices = null!;



    protected override void OnGameStart()
    {
        _indices = new Vector2Int[Size.x * Size.y];
        int i = 0;
        for(int x = 0; x < Size.x; ++x)
        {
            for(int y = 0; y < Size.y; ++y)
            {
                var index = new Vector2Int(x, y);
                _indices[i++] = index;
            }
        }

        _timeSinceBlockSpawned = 0;
    }

    protected override void OnGameStop()
    {
        foreach(var (_, block) in _notGroundedBlocks)
            block.GameObject.Destroy();
        _notGroundedBlocks.Clear();
    }



    protected override void OnValidate()
    {
        if(Size.x < 1)
            Size = Size.WithX(1);
        if(Size.y < 1)
            Size = Size.WithY(1);
    }
    protected override void OnUpdate()
    {
        base.OnUpdate();

        if(IsProxy || Status != GameStatus.Started)
            return;

        UpdateNotGroundedBlocks();

        var blocksToSpawn = (_timeSinceBlockSpawned / (1f / SpawningSpeed)).FloorToInt();

        if(blocksToSpawn <= 0)
            return;

        _timeSinceBlockSpawned -= (1f / SpawningSpeed) * blocksToSpawn;
        SpawnRandomBlocks(blocksToSpawn);
    }



    private void UpdateNotGroundedBlocks()
    {
        var groundedBlocks = _notGroundedBlocks.Where(f => f.Value.Grounded).ToArray();

        foreach(var (position, block) in groundedBlocks)
        {
            _notGroundedBlocks.Remove(position);
            _groundedBlocksCount[position] = _groundedBlocksCount.GetValueOrDefault(position, 0) + 1;
        }
    }

    private void SpawnRandomBlocks(int count)
    {
        var indices = _indices.Except(_notGroundedBlocks.Keys).OrderBy(x => Guid.NewGuid()).Take(count);

        foreach(var index in indices)
            SpawnBlock(index);
    }

    private void SpawnBlock(Vector2Int index)
    {
        if(_notGroundedBlocks.ContainsKey(index))
            throw new InvalidOperationException("There is still falling block at given index.");

        var position = new Vector3((index.x + 0.5f) * BlockSize.x * Consts.CubeModelSize.x,
            (index.y + 0.5f) * BlockSize.y * Consts.CubeModelSize.y,
            SpawningHeight + BlockSize.z * Consts.CubeModelSize.z * (0.5f + _groundedBlocksCount.GetValueOrDefault(index, 0)));

        var cloneConfig = new CloneConfig(new global::Transform(position, Transform.Rotation, BlockSize), GameObject, false);

        var fallingBlockGameObject = FallingBlockPrefab.Clone(cloneConfig);

        var fallingBlock = fallingBlockGameObject.Components.Get<FallingBlock>(true);

        _notGroundedBlocks.Add(index, fallingBlock);

        fallingBlock.Speed = BlocksSpeed;

        if(BlockColors.Count > 0)
            fallingBlock.Color = BlockColors[Game.Random.Next(BlockColors.Count)];

        fallingBlockGameObject.Enabled = true;

        fallingBlockGameObject.NetworkMode = NetworkMode.Object;
        fallingBlockGameObject.NetworkSpawn();
    }

    public override ISet<ulong> GetWinners()
    {
        if(Status != GameStatus.Stopped)
            throw new InvalidOperationException("Incorrect game status.");

        return PlayingPlayers.Select(p => p.Network.OwnerConnection.SteamId).ToHashSet();
    }
}
