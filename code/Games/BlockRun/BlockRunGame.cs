using Mini.Exceptions;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mini.Games.BlockRun;

public sealed class BlockRunGame : MiniGame
{
    [Property]
    public Vector3 BlockSize { get; set; } = new Vector3(1f, 1f, 0.25f);
    [Property]
    public float Gap { get; set; } = 4f;
    [Property, KeyProperty]
    public List<LevelInfo> LevelInfos { get; set; } = new();
    [Property]
    public float LevelHeight { get; set; } = 400f;
    [Property]
    public GameObject BlockPrefab { get; set; } = null!;
    [Property]
    public GameObject BlocksParent { get; set; } = null!;
    [Property]
    public GameObject SpawnPointPrefab { get; set; } = null!;
    [Property]
    public GameObject SpawnPointsParent { get; set; } = null!;
    [Property]
    public GameObject SpawnPlatform { get; set; } = null!;
    [Property]
    public GameObject KillingZone { get; set; } = null!;

    private Vector2Int _maxSize;


    public record struct LevelInfo
    {
        [Property]
        public Vector2Int Size { get; set; } = 1;
        [Property]
        public Color Color { get; set; } = Color.White;

        public LevelInfo()
        {
        }
    }

    protected override async Task OnGameSetup()
    {
        _maxSize = 0;
        foreach(var levelInfo in LevelInfos)
            _maxSize = _maxSize.ComponentMax(levelInfo.Size);

        SetupRoom();
        CreateSpawnPoints();

        await base.OnGameSetup();
        await SpawnBlocks();
    }

    protected override async Task OnGameStart()
    {
        await base.OnGameStart();
        EnableSpawnPlatform(false);
    }

    private void SetupRoom()
    {
        var maxHeight = LevelHeight * LevelInfos.Count + BlockSize.z * Consts.CubeModelSize;
        var maxWorldSize = _maxSize * Consts.CubeModelSize + (_maxSize - 1) * Gap;

        SpawnPlatform.Transform.Scale = new Vector3(maxWorldSize / Consts.CubeModelSize).WithZ(SpawnPlatform.Transform.Scale.z);
        SpawnPlatform.Transform.Position = Transform.Position +
            SpawnPlatform.Transform.Scale.WithZ(0f) / 2f * Consts.CubeModelSize +
            Vector3.Up * (maxHeight + LevelHeight);

        KillingZone.Transform.Position = SpawnPlatform.Transform.Position
            .WithZ(Transform.Position.z - KillingZone.Transform.Scale.z / 2f * Consts.CubeModelSize);
        KillingZone.Transform.Scale = (SpawnPlatform.Transform.Scale * 2).WithZ(KillingZone.Transform.Scale.z);
    }

    private void CreateSpawnPoints()
    {
        var offset = SpawnPlatform.Transform.Scale / 4 * Consts.CubeModelSize;
        for(int x = -1; x <= 1; ++x)
        {
            for(int y = -1; y <= 1; ++y)
            {
                Vector3 position = SpawnPlatform.Transform.Position + new Vector3(x, y, 0f) * offset;
                SpawnPointPrefab.Clone(SpawnPointsParent, position, Rotation.Identity, Vector3.One).NetworkSpawn();
            }
        }
    }

    [Broadcast(NetPermission.OwnerOnly)]
    private void EnableSpawnPlatform(bool enabled)
    {
        SpawnPlatform.Enabled = enabled;
    }

    private async Task SpawnBlocks()
    {
        for(int z = 0; z < LevelInfos.Count; ++z)
        {
            var levelInfo = LevelInfos[z];
            for(int x = 0; x < levelInfo.Size.x; ++x)
            {
                for(int y = 0; y < levelInfo.Size.y; ++y)
                {
                    SpawnBlock(new Vector3Int(x, y, z) + (_maxSize - levelInfo.Size) / 2);
                    await Task.Yield();
                }
            }
        }
    }

    private void SpawnBlock(Vector3Int index)
    {
        if(index.z < 0 || index.z >= LevelInfos.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        var position = new Vector3((index.x + 0.5f) * BlockSize.x * Consts.CubeModelSize + Gap * index.x,
            (index.y + 0.5f) * BlockSize.y * Consts.CubeModelSize + Gap * index.y,
            LevelHeight * (index.z + 1));

        var cloneConfig = new CloneConfig(new global::Transform(position, Transform.Rotation, BlockSize), BlocksParent, false);

        var blockGameObject = BlockPrefab.Clone(cloneConfig);
        var block = blockGameObject.Components.Get<BlockRunBlock>(true);
        if(!block.IsValid())
            throw new ComponentNotFoundException(blockGameObject, typeof(BlockRunBlock));

        block.Color = LevelInfos[index.z].Color;

        blockGameObject.Enabled = true;
        blockGameObject.NetworkSpawn();
    }

    protected override void OnValidate()
    {
        for(int i = 0; i < LevelInfos.Count; ++i)
        {
            var info = LevelInfos[i];
            if(info.Size.x >= 1 && info.Size.y >= 1)
                continue;

            if(info.Size.x < 1)
                info.Size = info.Size.WithX(1);
            if(info.Size.y < 1)
                info.Size = info.Size.WithY(1);

            LevelInfos[i] = info;
        }
    }
}
