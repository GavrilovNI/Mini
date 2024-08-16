using Mini.Games;
using Sandbox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Mini;

public class GamesLoader : Component
{
    public const string GameInfoFileName = "info.json";
    public const string GamePrefabFileName = "game.prefab";
    public const string GameIconFileName = "icon.png";

    [Sync]
    private NetDictionary<string, LoadedGameInfo> NetGames { get; set; } = new();

    public IEnumerable<LoadedGameInfo> Games => NetGames.Values;
    public int GamesCount => NetGames.Count;

    protected override void OnStart()
    {
        if(IsProxy)
            return;

        LoadGames("games");
    }

    [Button("Load Games")]
    public void LoadGamesBtn() => LoadGames("games");

    public void LoadGames(string path)
    {
        var fileSystem = FileSystem.Mounted.CreateSubSystem(path);
        var directories = fileSystem.FindDirectory("/");

        HashSet<string> loadedIdentities = new();

        int loadedGamesCount = 0;
        foreach (var directory in directories)
        {
            var gameId = directory.ToLower();
            if(loadedIdentities.Contains(gameId))
                continue;

            var gameFileSystem = fileSystem.CreateSubSystem(gameId);
            var gameIsValid = gameFileSystem.FileExists(GameInfoFileName) && gameFileSystem.FileExists(GamePrefabFileName);

            if(!gameIsValid)
                continue;

            var gameInfoText = gameFileSystem.ReadAllText(GameInfoFileName);

            GameInfo gameInfo;
            try
            {
                gameInfo = Json.Deserialize<GameInfo>(gameInfoText);
            }
            catch(JsonException ex)
            {
                Log.Error($"Couldn't read game info. {ex}");
                continue;
            }

            NetGames[gameId] = new LoadedGameInfo()
            {
                GameId = gameId,
                GameInfo = gameInfo,
                Path = Path.Combine(path, gameId)
            };
            loadedGamesCount++;

            loadedIdentities.Add(gameId);
        }

        Log.Info($"Loaded {loadedGamesCount} games.");
    }

    [Button("Clear")]
    public void Clear() => NetGames.Clear();

    public GameObject CloneGamePrefab(string gameId, CloneConfig cloneConfig)
    {
        if(!NetGames.TryGetValue(gameId, out var gameData))
            throw new InvalidOperationException($"Game {gameId} wasn't loaded.");

        return PrefabScene.Clone(Path.Combine(gameData.Path, GamePrefabFileName), cloneConfig);
    }

    public GameInfo GetGameInfo(string gameId)
    {
        if(!NetGames.TryGetValue(gameId, out var gameData))
            throw new InvalidOperationException($"Game {gameId} wasn't loaded.");

        return gameData.GameInfo;
    }
}
