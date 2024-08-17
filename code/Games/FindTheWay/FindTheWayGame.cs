using Mini.Exceptions;
using Mini.Games.FallingBlocks;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mini.Games.FindTheWay;

public class FindTheWayGame : MiniGame
{
    [Property]
    public Vector2Int Size { get; set; } = new Vector2Int(20, 8);

    [Property]
    public Vector3 BlockSize { get; set; } = new Vector3(3f, 3f, 1f);

    [Property]
    public GameObject FakeBlockPrefab { get; set; } = null!;
    [Property]
    public GameObject HighlightedBlockPrefab { get; set; } = null!;

    [Property]
    public Color Color { get; set; } = new Color(0.33f, 0.33f, 0.33f);
    [Property]
    public Color HighlightColor { get; set; } = Color.White;

    [Property]
    public float TurnChanceMultiplier = 3f;
    [Property]
    public float TimeBetweenHighlights = 15f;
    [Property]
    public float MinDistanceWithoutTurn = 2f;


    private readonly Dictionary<Vector2Int, FindTheWayBlock> _blocks = new();

    protected override async Task OnGameSetup()
    {
        await base.OnGameSetup();
        await BuildBlocks(CancellationToken.None);
    }

    [Button("Rebuild"), Group("Debug")]
    private void BuildBlocksBtn() => BuildBlocks(CancellationToken.None);

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
        Vector2Int[] sideOffsets = new Vector2Int[4] { Vector2Int.Up, Vector2Int.Right, Vector2Int.Down, Vector2Int.Left };

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
                var currentSideOffsetAndPoses = sideOffsets.Select(offset => (offset, position: oldPos + offset))
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

        var position = new Vector3((index.x + 0.5f) * BlockSize.x * Consts.CubeModelSize.x,
            (index.y + 0.5f) * BlockSize.y * Consts.CubeModelSize.y, 0f);

        var cloneConfig = new CloneConfig(new global::Transform(position, Transform.Rotation, BlockSize), GameObject, false);

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
        if(!fake)
            block.Color = Color.Green;

        blockGameObject.Enabled = true;

        blockGameObject.NetworkMode = NetworkMode.Object;
        blockGameObject.NetworkSpawn();

        _blocks.Add(index, block);
    }


    protected override void OnValidate()
    {
        if(Size.x < 1)
            Size = Size.WithX(1);
        if(Size.y < 1)
            Size = Size.WithY(1);
    }
}
