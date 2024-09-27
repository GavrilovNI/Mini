using Mini.Settings;
using Sandbox;
using System;

namespace Mini.Players;

public sealed class PlayerCameraController : Component
{
    [Property]
    public GameObject Camera { get; private set; } = null!;
    [Property]
    public GameObject Eye { get; private set; } = null!;
    [Property]
    public PlayerVisibilityController? VisibilityController { get; set; }

    [Property]
    public bool IsFirstPerson { get; private set; } = true;
    [Property]
    public bool AllowChangingView { get; set; } = true;
    [Property, HideIf(nameof(IsFirstPerson), true)]
    public float MinBackingDistance { get; set; } = 100;
    [Property, HideIf(nameof(IsFirstPerson), true)]
    public float MaxBackingDistance { get; set; } = 300;
    [Property, HideIf(nameof(IsFirstPerson), true)]
    public float BackingDistanceChangeSpeed { get; set; } = 10;


    public float BackingDistance { get; private set; }


    public void SetView(bool firstPerson)
    {
        IsFirstPerson = firstPerson;
        ClipBack();
    }

    protected override void OnAwake()
    {
        BackingDistance = MinBackingDistance;
    }

    protected override void OnUpdate()
    {
        if(!Network.IsProxy)
        {
            if(Input.Pressed("View") && AllowChangingView)
            {
                SetView(!IsFirstPerson);
                VisibilityController?.UpdateVisibility();
            }

            UpdateBackingDistance();
            Rotate();
            ClipBack();
        }
    }

    private void UpdateBackingDistance()
    {
        BackingDistance = Math.Clamp(BackingDistance -= Input.MouseWheel.y * BackingDistanceChangeSpeed, MinBackingDistance, MaxBackingDistance);
    }

    private void Rotate()
    {
        var sensitivity = GameSettings.Current.Sensitivity;
        sensitivity = 10f * sensitivity / Math.Max(1f, MathF.Max(Screen.Width, Screen.Height) * 0.5f);

        var eyeAngles = Eye.Transform.Rotation.Angles();
        eyeAngles.pitch += Input.MouseDelta.y * sensitivity;
        eyeAngles.yaw -= Input.MouseDelta.x * sensitivity;
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
        if(MinBackingDistance < 0f)
            MinBackingDistance = 0f;
        if(MaxBackingDistance < MinBackingDistance)
            MaxBackingDistance = MinBackingDistance;
    }
}
