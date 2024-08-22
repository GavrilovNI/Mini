using Mini.Exceptions;
using Mini.Games.FallingBlocks;
using Mini.Players;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mini.Games.FindTheWay;

public class FindTheWayGame : MiniGame
{
    private static readonly Vector2Int[] _sideOffsets = new Vector2Int[4] { Vector2Int.Up, Vector2Int.Right, Vector2Int.Down, Vector2Int.Left };

    [Property]
    public Vector2Int Size { get; set; } = new Vector2Int(20, 8);

    [Property]
    public Vector3 BlockSize { get; set; } = new Vector3(3f, 3f, 1f);

    [Property]
    public GameObject FakeBlockPrefab { get; set; } = null!;
    [Property]
    public GameObject HighlightedBlockPrefab { get; set; } = null!;
    [Property]
    public GameObject SpawnPointPrefab { get; set; } = null!;
    [Property]
    public GameObject SpawnPointParent { get; set; } = null!;
    [Property]
    public GameObject BlocksParent { get; set; } = null!;
    [Property]
    public GameObject SpawnPlatform { get; set; } = null!;
    [Property]
    public GameObject FinishPlatform { get; set; } = null!;
    [Property]
    public GameObject KillingZone { get; set; } = null!;
    [Property]
    public GameObject SpawnBarrier { get; set; } = null!;

    [Property, Group("Rendering")]
    public Color Color { get; set; } = new Color(0.33f, 0.33f, 0.33f);
    [Property, Group("Rendering")]
    public Color HighlightColor { get; set; } = Color.White;
    [Property, Group("Rendering")]
    public float HighlightSpeed { get; set; } = 5f;
    [Property, Group("Rendering")]
    public float HighlightTime { get; set; } = 2f;
    [Property, Group("Rendering")]
    public Curve HighlightCurve { get; set; } = 2f;
    [Property, Group("Rendering")]
    public float TimeBetweenHighlights { get; set; } = 10f;

    [Property, Group("Path")]
    public float TurnChanceMultiplier { get; set; } = 3f;
    [Property, Group("Path")]
    public float MinDistanceWithoutTurn { get; set; } = 2f;

    private readonly Dictionary<Vector2Int, FindTheWayBlock> _blocks = new();
    private Task? _highlightTask = null;
    private TimeSince _timeSinceHightlight;

    private HashSet<ulong> _finishedPlayers = new();


    protected override async Task OnGameSetup()
    {
        _timeSinceHightlight = Time.Now;

        UpdatePlatforms();
        CreateSpawnPoints();

        FinishPlatform.Components.GetAll<Collider>().First(c => c.IsTrigger).OnTriggerEnter = OnFinishEnter;

        await base.OnGameSetup();
        await BuildBlocks(CancellationToken.None);
    }

    protected override async Task OnGameStart()
    {
        await base.OnGameStart();
        EnableBarrier(false);
    }

    [Broadcast(NetPermission.OwnerOnly)]
    private void EnableBarrier(bool enabled)
    {
        SpawnBarrier.Enabled = enabled;
    }

    private void OnFinishEnter(Collider collider)
    {
        if(IsProxy)
            return;

        var player = collider.Components.Get<Player>();

        if(player.IsValid())
            _finishedPlayers.Add(player.Network.OwnerConnection.SteamId);
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        if(IsProxy)
            return;

        if(Status != GameStatus.Started)
            return;

        if(_highlightTask is null)
        {
            if(_timeSinceHightlight >= TimeBetweenHighlights)
                _highlightTask = HighlightPath(CancellationToken.None);
        }
        else if(_highlightTask.IsCompleted)
        {
            _timeSinceHightlight = 0;
            _highlightTask = null;
        }
    }


    [Button("Update Platforms"), Group("Debug")]
    [Broadcast(NetPermission.OwnerOnly)]
    private void UpdatePlatforms()
    {
        SpawnPlatform.Transform.Scale = SpawnPlatform.Transform.World.Scale.WithY(BlockSize.y * Size.y);
        SpawnPlatform.Transform.Position = SpawnPlatform.Transform.Position
            .WithY(SpawnPlatform.Transform.Scale.y / 2f * Consts.CubeModelSize);

        SpawnBarrier.Transform.Scale = SpawnPlatform.Transform.World.Scale.WithZ(SpawnBarrier.Transform.Scale.z);
        SpawnBarrier.Transform.Position = SpawnBarrier.Transform.Scale.WithX(-SpawnBarrier.Transform.Scale.x).WithZ(0f) * Consts.CubeModelSize / 2f;

        FinishPlatform.Transform.Scale = FinishPlatform.Transform.World.Scale.WithY(BlockSize.y * Size.y);
        FinishPlatform.Transform.Position = FinishPlatform.Transform.World.Position
            .WithY(SpawnPlatform.Transform.Scale.y / 2f * Consts.CubeModelSize)
            .WithX(SpawnPlatform.Transform.Scale.x / 2f * Consts.CubeModelSize + BlockSize.x * Size.x * Consts.CubeModelSize);

        KillingZone.Transform.Scale = KillingZone.Transform.Scale.WithX(BlockSize.x * Size.x * 2).WithY(BlockSize.y * Size.y * 2);
        KillingZone.Transform.Position = KillingZone.Transform.Position.WithY(SpawnPlatform.Transform.Position.y)
            .WithX(BlockSize.x * Size.x * Consts.CubeModelSize / 2f);
    }

    [Button("Recreate Spawn Points"), Group("Debug")]
    private void CreateSpawnPoints()
    {
        foreach(var spawnPoint in GameObject.Components.GetAll<SpawnPoint>(FindMode.EverythingInSelfAndDescendants))
            spawnPoint.GameObject.Destroy();

        for(int i = 0; i < Size.y; ++i)
        {
            var position = new Vector3(-SpawnPlatform.Transform.Scale.x * Consts.CubeModelSize / 2f, BlockSize.y * Consts.CubeModelSize * (i + 0.5f), SpawnPlatform.Transform.Scale.z * Consts.CubeModelSize / 2f);
            SpawnPointPrefab.Clone(SpawnPointParent, position, Rotation.Identity, Vector3.One).NetworkSpawn();  
        }

        UpdateSpawnPoints();
    }

    [Button("Rebuild"), Group("Debug")]
    private void BuildBlocksBtn() => _ = BuildBlocks(CancellationToken.None);

    private async Task BuildBlocks(CancellationToken cancellationToken)
    {
        foreach(var (pos, _) in _blocks)
            DestroyBlock(pos);

        await SpawnValidBlocks(cancellationToken);
        await FillWithFakeBlocks(cancellationToken);
    }

    private static int GetRandomIndexByChances(IEnumerable<float> chances)
    {
        if(!chances.Any())
            throw new ArgumentException("Sequence doesn't conrain elements.", nameof(chances));

        var sum = chances.Sum();
        var randomValue = Game.Random.Float(sum);

        int index = 0;
        foreach(var chance in chances)
        {
            randomValue -= chance;
            if(randomValue < 0)
                return index;
            index++;
        }

        return index - 1;
    }

    private Task SpawnValidBlocks(CancellationToken cancellationToken) => SpawnValidBlocks(new Vector2Int(0, Game.Random.Next(Size.y)), cancellationToken);

    private async Task SpawnValidBlocks(Vector2Int startPos, CancellationToken cancellationToken)
    {
        var oldOffset = Vector2Int.Up;
        var oldPos = startPos;
        SpawnBlock(oldPos, false);

        var closestToEndPosition = oldPos;

        var lastPositions = new List<Vector2Int>() { oldPos };

        var distanceWithoutTurn = 1;

        do
        {
            if(cancellationToken.IsCancellationRequested)
                return;

            Vector2Int newPos;
            Vector2Int newOffset;

            if(distanceWithoutTurn < MinDistanceWithoutTurn && IsValidToPlace(oldPos + oldOffset))
            {
                newPos = oldPos + oldOffset;
                newOffset = oldOffset;
            }
            else
            {
                var currentSideOffsetAndPoses = _sideOffsets.Select(offset => (offset, position: oldPos + offset))
                    .Where(x => IsValidToPlace(x.position));

                if(!currentSideOffsetAndPoses.Any())
                    break;

                var sideChances = currentSideOffsetAndPoses.Select(x => (x.offset == oldOffset || x.offset == Vector2Int.Zero - oldOffset) ? 1f : TurnChanceMultiplier);
                var randomIndex = GetRandomIndexByChances(sideChances);
                var newOffsetAndPos = currentSideOffsetAndPoses.Skip(randomIndex).First();
                newPos = newOffsetAndPos.position;
                newOffset = newOffsetAndPos.offset;
            }

            SpawnBlock(newPos, false);
            await Task.Yield();

            bool turned = newOffset != oldOffset && newOffset != Vector2Int.Zero - oldOffset;
            distanceWithoutTurn = turned ? 1 : distanceWithoutTurn + 1;

            lastPositions.Add(newPos);
            if(lastPositions.Count > 2)
                lastPositions.RemoveAt(0);

            if(newPos.x > closestToEndPosition.x)
                closestToEndPosition = newPos;

            oldPos = newPos;
            oldOffset = newOffset;

            if(newPos.x + 1 == Size.x)
                break;
        }
        while(true);

        if(oldPos.x + 1 != Size.x)
            await SpawnValidBlocks(closestToEndPosition + Vector2Int.Right, cancellationToken);

        bool IsValidToPlace(Vector2Int position)
        {
            if(position.x < 0 || position.y < 0 || position.x >= Size.x || position.y >= Size.y)
                return false;

            if(_blocks.ContainsKey(position))
                return false;

            var neighborCount = 0;
            for(int x = -1; x <= 1; ++x)
            {
                for(int y = -1; y <= 1; ++y)
                {
                    var neighborPos = position + new Vector2Int(x, y);
                    if(_blocks.ContainsKey(neighborPos))
                    {
                        if(!lastPositions!.Contains(neighborPos))
                            return false;
                        neighborCount++;
                    }
                }
            }
            return neighborCount <= 2;
        }
    }

    private async Task FillWithFakeBlocks(CancellationToken cancellationToken)
    {
        for(int x = 0; x < Size.x; ++x)
        {
            for(int y = 0; y < Size.y; ++y)
            {
                var fillPos = new Vector2Int(x, y);
                if(!_blocks.ContainsKey(fillPos))
                {
                    if(cancellationToken.IsCancellationRequested)
                        return;

                    SpawnBlock(fillPos, true);
                    await Task.Yield();
                }
            }
        }
    }

    private void DestroyBlock(Vector2Int index)
    {
        if(!_blocks.TryGetValue(index, out var existingBlock) || !existingBlock.IsValid())
            throw new InvalidOperationException($"Block at {index} was not spawned.");

        existingBlock.GameObject.Destroy();
        _blocks.Remove(index);
    }

    private void SpawnBlock(Vector2Int index, bool fake)
    {
        if(_blocks.TryGetValue(index, out var existingBlock) && existingBlock.IsValid())
            throw new InvalidOperationException($"Block at {index} was already spawned.");

        var position = new Vector3((index.x + 0.5f) * BlockSize.x * Consts.CubeModelSize,
            (index.y + 0.5f) * BlockSize.y * Consts.CubeModelSize, 0f);

        var cloneConfig = new CloneConfig(new global::Transform(position, Transform.Rotation, BlockSize), BlocksParent, false);

        var prefab = fake ? FakeBlockPrefab : HighlightedBlockPrefab;
        var blockGameObject = prefab.Clone(cloneConfig);
        var block = blockGameObject.Components.Get<FindTheWayBlock>(true);

        if(!block.IsValid())
            throw new ComponentNotFoundException(blockGameObject, typeof(FindTheWayBlock));
        if(fake && block is not FakeBlock)
            throw new ComponentNotFoundException(blockGameObject, typeof(FakeBlock));
        if(!fake && block is not HighlightedBlock)
            throw new ComponentNotFoundException(blockGameObject, typeof(HighlightedBlock));

        block.Setup(this);

        blockGameObject.Enabled = true;

        blockGameObject.NetworkMode = NetworkMode.Object;
        blockGameObject.NetworkSpawn();

        _blocks.Add(index, block);
    }

    [Button("Highlight Path"), Group("Debug")]
    private void HighlightPathBtn() => _ = HighlightPath(CancellationToken.None);

    private async Task HighlightPath(CancellationToken cancellationToken)
    {
        List<Task> tasks = new();
        HashSet<Vector2Int> hightLightedBlocks = new();

        for(int y = 0; y < Size.y; ++y)
            tasks.Add(HighlightPath(new Vector2Int(0, y), hightLightedBlocks, cancellationToken));

        await Task.WhenAll(tasks);
    }

    private async Task HighlightPath(Vector2Int startPos, HashSet<Vector2Int> hightLightedBlocks, CancellationToken cancellationToken)
    {
        if(hightLightedBlocks.Contains(startPos))
            return;

        hightLightedBlocks.Add(startPos);

        List<Task> tasks = new();

        if(_blocks.TryGetValue(startPos, out var block) && block is HighlightedBlock highlightedBlock)
            highlightedBlock.Highlight();
        else
            return;

        await Task.DelaySeconds(1f / HighlightSpeed, cancellationToken);

        foreach(var offset in _sideOffsets)
            tasks.Add(HighlightPath(startPos + offset, hightLightedBlocks, cancellationToken));

        await Task.WhenAll(tasks);
    }

    protected override ISet<ulong> ChooseWinners() => _finishedPlayers;

    protected override void OnValidate()
    {
        if(Size.x < 1)
            Size = Size.WithX(1);
        if(Size.y < 1)
            Size = Size.WithY(1);
    }
}
