using Sandbox;
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
    }

    [Button("Highlight")]
    private void HighlightBtn() => _ = Highlight(HighlightTime, CancellationToken.None);

    public Task Highlight(CancellationToken cancellationToken) => Highlight(HighlightTime, cancellationToken);
    public async Task Highlight(float time, CancellationToken cancellationToken)
    {
        TimeSince timeSinceStart = 0;

        while(timeSinceStart < time)
        {
            Color color = Color.Lerp(Color, HighlightColor, HighlightCurve.Evaluate(timeSinceStart / time));
            ModelRenderer.Tint = color;
            await Task.Frame();

            if(cancellationToken.IsCancellationRequested)
            {
                ModelRenderer.Tint = Color;
                return;
            }
        }

        ModelRenderer.Tint = Color;
    }
}
