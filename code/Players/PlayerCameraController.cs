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


    private float _backingDistance;


    public void SetView(bool firstPerson)
    {
        IsFirstPerson = firstPerson;
        ClipBack();
    }

    protected override void OnAwake()
    {
        _backingDistance = MinBackingDistance;
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
        _backingDistance = Math.Clamp(_backingDistance -= Input.MouseWheel.y * BackingDistanceChangeSpeed, MinBackingDistance, MaxBackingDistance);
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
        var traceResult = Scene.Trace.Ray(Eye.Transform.Position, Eye.Transform.Position + cameraBackward * _backingDistance)
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
