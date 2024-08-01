using Sandbox;

namespace Mini.Player;

public sealed class PlayerCameraController : Component
{
    [Property]
    private CameraComponent Camera { get; set; } = null!;
    [Property]
    private GameObject Eye { get; set; } = null!;

    [Property]
    public bool IsFirstPerson { get; set; } = true;
    [Property, HideIf(nameof(IsFirstPerson), true)]
    public float BackingDistance { get; set; } = 100;

    [Property]
    private ModelRenderer? Model { get; set; }


    protected override void OnUpdate()
    {
        Rotate();
        ClipBack();

        if(Model.IsValid())
            Model.RenderType = IsFirstPerson ? ModelRenderer.ShadowRenderType.ShadowsOnly : ModelRenderer.ShadowRenderType.On;
    }

    private void Rotate()
    {
        var eyeAngles = Eye.Transform.Rotation.Angles();
        eyeAngles.pitch += Input.MouseDelta.y * 0.1f;
        eyeAngles.yaw -= Input.MouseDelta.x * 0.1f;
        eyeAngles.roll = 0f;
        eyeAngles.pitch = eyeAngles.pitch.Clamp(-89f, 89f);
        Eye.Transform.Rotation = eyeAngles.ToRotation();
    }

    private void ClipBack()
    {
        if(IsFirstPerson)
        {
            Camera.Transform.LocalPosition = Camera.Transform.LocalPosition.WithX(0f);
            return;
        }

        var cameraBackward = Camera.Transform.Rotation.Backward;
        var traceResult = Scene.Trace.Ray(Eye.Transform.Position, Eye.Transform.Position + cameraBackward * BackingDistance)
                                    .WithCollisionRules("player")
                                    .IgnoreGameObject(GameObject)
                                    .Radius(1f)
                                    .Run();
        Camera.Transform.LocalPosition = Camera.Transform.LocalPosition.WithX(-traceResult.Distance);
    }

    protected override void OnValidate()
    {
        if(BackingDistance < 0f)
            BackingDistance = 0f;
    }
}
