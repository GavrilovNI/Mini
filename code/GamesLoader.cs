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

    private readonly Dictionary<string, (GameInfo GameInfo, string Path)> _games = new();

    public IEnumerable<KeyValuePair<string, (GameInfo GameInfo, string Path)>> Games => _games;
    public int GamesCount => _games.Count;

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
            var gameIdentity = directory.ToLower();
            if(loadedIdentities.Contains(gameIdentity))
                continue;

            var gameFileSystem = fileSystem.CreateSubSystem(gameIdentity);
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

            _games[gameIdentity] = (gameInfo, Path.Combine(path, gameIdentity));
            loadedGamesCount++;

            loadedIdentities.Add(gameIdentity);
        }

        Log.Info($"Loaded {loadedGamesCount} games.");
    }

    [Button("Clear")]
    public void Clear() => _games.Clear();

    public GameObject CloneGamePrefab(string gameIdentity, CloneConfig cloneConfig)
    {
        if(!_games.TryGetValue(gameIdentity, out var gameData))
            throw new InvalidOperationException($"Game {gameIdentity} wasn't loaded.");

        return PrefabScene.Clone(Path.Combine(gameData.Path, GamePrefabFileName), cloneConfig);
    }

    public GameInfo GetGameInfo(string gameIdentity)
    {
        if(!_games.TryGetValue(gameIdentity, out var gameData))
            throw new InvalidOperationException($"Game {gameIdentity} wasn't loaded.");

        return gameData.GameInfo;
    }
}
