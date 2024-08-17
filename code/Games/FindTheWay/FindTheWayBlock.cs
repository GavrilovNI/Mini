using Sandbox;

namespace Mini.Games.FindTheWay;

public abstract class FindTheWayBlock : Component
{
    [Property]
    [RequireComponent]
    public ModelRenderer ModelRenderer { get; set; } = null!;

    [Property]
    public Color Color { get; set; } = new Color(0.33f, 0.33f, 0.33f);

    public virtual void Setup(FindTheWayGame game)
    {
        Color = game.Color;
    }

    protected override void OnStart()
    {
        ModelRenderer.Tint = Color;
    }
}
