using Sandbox;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Globalization;
using Sandbox.Audio;
using System.Linq;

namespace Mini.Settings;

[Serializable]
public class GameSettings
{
    public const string DefaultPath = "settings.json";

    private static GameSettings? _current;
    public static GameSettings Current => _current ??= Load(DefaultPath);


    [DefaultValue(8f)]
    public float Sensitivity { get; set; } = 8f;

#pragma warning disable IDE1006 // Naming Styles
    [JsonPropertyName("SoundVolumes")]
    public Dictionary<string, float> _soundVolumes { get; set; } = new(); // TODO: make it private
#pragma warning restore IDE1006 // Naming Styles


    public GameSettings()
    {
        _soundVolumes.TryAdd("master", 1f);
        _soundVolumes.TryAdd("music", 1f);
        _soundVolumes.TryAdd("game", 1f);
        _soundVolumes.TryAdd("ui", 1f);
        _soundVolumes.TryAdd("voice", 1f);
    }

    public float GetSoundVolume(string name) => _soundVolumes.GetValueOrDefault(name.ToLower(), 1f);
    public void SetSoundVolume(string name, float value)
    {
        if(value < 0f || value > 1f)
            throw new ArgumentOutOfRangeException(nameof(value));

        _soundVolumes[name.ToLower()] = value;
    }

    public void Save(string path)
    {
        var json = Json.Serialize(this);
        FileSystem.Data.WriteAllText(path, json);
    }

    public static GameSettings Load(string path)
    {
        if(!FileSystem.Data.FileExists(path))
            return new();

        var json = FileSystem.Data.ReadAllText(path);
        return Json.Deserialize<GameSettings>(json);
    }
}
