using Sandbox;

namespace Mini.Players;

public class PlayerVisibilityController : Component, Component.INetworkSpawn
{
    [Property]
    public PlayerCameraController CameraController { get; set; } = null!;
    [Property]
    public PlayerCameraController? CameraControllerOverride { get; set; }
    [Property]
    public GameObject Model { get; set; } = null!;


    public void OnNetworkSpawn(Connection owner)
    {
        UpdateVisibility();
    }

    protected override void OnStart()
    {
        UpdateVisibility();
    }

    protected override void OnUpdate()
    {
        UpdateVisibility();
    }

    public void UpdateVisibility()
    {
        if(!Model.IsValid())
            return;

        var cameraController = CameraControllerOverride ?? CameraController;

        ModelRenderer.ShadowRenderType renderType;
        if(cameraController.IsProxy)
        {
            renderType = ModelRenderer.ShadowRenderType.On;
        }
        else
        {
            renderType = cameraController.IsFirstPerson ?
                ModelRenderer.ShadowRenderType.ShadowsOnly :
                ModelRenderer.ShadowRenderType.On;
        }

        foreach(var model in Model.Components.GetAll<ModelRenderer>(FindMode.EverythingInSelfAndDescendants))
            model.RenderType = renderType;
    }
}
