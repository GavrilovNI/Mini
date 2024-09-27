using Sandbox;
using System.Linq;
using static Sandbox.Component;

namespace Mini.Players;

public sealed class PlayerDresser : Component, INetworkSpawn
{
    [Property]
    public SkinnedModelRenderer Model { get; set; } = null!;

    public void OnNetworkSpawn(Connection owner)
    {
        if(IsProxy)
            return;

        var clothing = ClothingContainer.CreateFromJson(owner.GetUserData("avatar"));
        UpdateClothing(clothing);
    }

    protected override void OnStart()
    {
        if(!Network.Active)
            UpdateClothing(ClothingContainer.CreateFromLocalUser());
    }

    private void UpdateClothing(ClothingContainer clothing)
    {
        clothing.Apply(Model);
    }
}
