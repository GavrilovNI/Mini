using Sandbox;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mini;

public class LocalPrefabsSpawner : Component
{
    public struct SpawnSettings
    {
        [Property]
        public GameObject Prefab { get; set; }
        [Property]
        public bool StartEnabled { get; set; }
        [Property]
        public Transform Transform { get; set; }
        [Property]
        public string? Name { get; set; }
        [Property]
        public GameObject? Parent { get; set; }
        [Property]
        public Dictionary<string, object> PrefabVariables { get; set; }

        public CloneConfig ToCloneConfig() => new()
        {
            StartEnabled = StartEnabled,
            Transform = Transform,
            Name = Name,
            Parent = Parent,
            PrefabVariables = PrefabVariables.ToDictionary(x => x.Key, x => x.Value)
        };
    }

    public enum PrefabsSpawnTime
    {
        OnLoad,
        OnAwake,
        OnStart
    }

    [Property]
    public List<SpawnSettings> Prefabs { get; set; } = new();
    [Property]
    public PrefabsSpawnTime SpawnTime { get; set; } = PrefabsSpawnTime.OnStart;


    [Button("Spawn prefabs")]
    public void SpawnPrefabs()
    {
        foreach(var spawnSettings in Prefabs)
        {
            var gameObject = spawnSettings.Prefab.Clone(spawnSettings.ToCloneConfig());
            gameObject.NetworkMode = NetworkMode.Never;
        }
    }

    protected override Task OnLoad()
    {
        if(Scene.IsEditor)
            return Task.CompletedTask;

        if(SpawnTime == PrefabsSpawnTime.OnLoad)
            SpawnPrefabs();
        return Task.CompletedTask;
    }

    protected override void OnAwake()
    {
        if(SpawnTime == PrefabsSpawnTime.OnAwake)
            SpawnPrefabs();
    }
    protected override void OnStart()
    {
        if(SpawnTime == PrefabsSpawnTime.OnStart)
            SpawnPrefabs();
    }
}
