﻿using Sandbox;
using System.Threading;
using System.Threading.Tasks;

namespace Mini.Games.FindTheWay;

public class HighlightedBlock : FindTheWayBlock
{
    [Property]
    public Color HighlightColor { get; set; } = Color.White;

    [Property]
    public Curve HighlightCurve { get; set; }

    [Property]
    public float HighlightTime { get; set; } = 3f;


    public override void Setup(FindTheWayGame game)
    {
        base.Setup(game);
        HighlightColor = game.HighlightColor;
        HighlightTime = game.HighlightTime;
        HighlightCurve = game.HighlightCurve;
    }

    [Button("Highlight")]
    [Broadcast(NetPermission.OwnerOnly)]
    public void Highlight() => _ = HighlightLocally();

    public async Task HighlightLocally()
    {
        TimeSince timeSinceStart = 0;

        while(timeSinceStart < HighlightTime)
        {
            Color color = Color.Lerp(Color, HighlightColor, HighlightCurve.Evaluate(timeSinceStart / HighlightTime));
            ModelRenderer.Tint = color;
            await Task.Frame();
        }

        ModelRenderer.Tint = Color;
    }
}
