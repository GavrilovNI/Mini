using Sandbox;
using System;

namespace Mini.Games.RedLightGreenLight;

public class IndicatingLight : Component
{
    public enum LightColor
    {
        Green,
        Yellow,
        Red
    }

    [Property, RequireComponent]
    public ModelRenderer Model { get; set; } = null!;
    [Property]
    public Color RedColor { get; set; } = Color.Red;
    [Property]
    public Color YellowColor { get; set; } = Color.Yellow;
    [Property]
    public Color GreenColor { get; set; } = Color.Green;
    [Property]
    public float MinTimeToChangeLight { get; set; } = 0.3f;
    [Property]
    public float MaxTimeToChangeLight { get; set; } = 3f;
    [Property]
    public float YellowColorTime { get; set; } = 0.7f;
    [Property]
    public bool Paused { get; set; } = false;
    [Sync]
    public LightColor CurrentColor { get; private set; } = LightColor.Red;

    private TimeUntil _timeUntilLightChange;


    [Broadcast(NetPermission.OwnerOnly)]
    public void SetColor(LightColor color)
    {
        Model.Tint = color switch
        {
            LightColor.Red => RedColor,
            LightColor.Yellow => YellowColor,
            LightColor.Green => GreenColor,
            _ => throw new ArgumentException("Unknown color.", nameof(color))
        };
        if(!IsProxy)
        {
            CurrentColor = color;
            if(color == LightColor.Yellow)
                _timeUntilLightChange = YellowColorTime;
            else
                _timeUntilLightChange = Game.Random.Float(MinTimeToChangeLight, MaxTimeToChangeLight);
        }
    }

    protected override void OnAwake()
    {
        if(!IsProxy)
            SetColor(CurrentColor);
    }

    public void ChangeLight()
    {
        var color = CurrentColor switch
        {
            LightColor.Red => LightColor.Green,
            LightColor.Yellow => LightColor.Red,
            LightColor.Green => LightColor.Yellow,
            _ => LightColor.Yellow
        };

        SetColor(color);
    }

    protected override void OnUpdate()
    {
        if(IsProxy || Paused)
            return;

        if(_timeUntilLightChange <= 0)
            ChangeLight();
    }


    protected override void OnValidate()
    {
        if(MinTimeToChangeLight < 0)
            MinTimeToChangeLight = 0;
        if(MaxTimeToChangeLight < MinTimeToChangeLight)
            MaxTimeToChangeLight = MinTimeToChangeLight;
    }
}
