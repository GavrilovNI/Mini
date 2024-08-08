using Sandbox;
using Sandbox.Citizen;
using System;

namespace Mini.Player;

public sealed class PlayerMovementController : Component
{
    [RequireComponent]
    [Property]
    private CharacterController CharacterController { get; set; } = null!;

    [RequireComponent]
    [Property]
    private BoxCollider Collider { get; set; } = null!;

    [Property]
    private CitizenAnimationHelper? AnimationHelper { get; set; }
    [Property]
    private ModelRenderer? Model { get; set; }

    [Property, Group("Camera")]
    private GameObject Eye { get; set; } = null!;

    [Property, Group("Camera")]
    public float EyeHeightOffset { get; set; } = -8f;


    [Property, Group("Movement")]
    public float GroundFriction { get; set; } = 4f;
    [Property, Group("Movement")]
    public float AirFriction { get; set; } = 0.1f;
    [Property, Group("Movement")]
    public float MaxForce { get; set; } = 50f;
    [Property, Group("Movement")]
    public float WalkingSpeed { get; set; } = 160f;
    [Property, Group("Movement")]
    public float RunningSpeed { get; set; } = 290f;
    [Property, Group("Movement")]
    public float CrouchingSpeed { get; set; } = 90f;
    [Property, Group("Movement")]
    public float JumpForce { get; set; } = 400f;
    [Property, Group("Movement")]
    public float StandingHeight { get; set; } = 72f;
    [Property, Group("Movement")]
    public float CrouchingHeight { get; set; } = 50f;
    [Property, Group("Movement")]
    public float HeightChangingSpeed { get; set; } = 0.005f;
    [Property, Group("Movement")]
    public float SkinSize { get; set; } = 1f;

    [Property, Group("Animation")]
    public float MaxModelDeltaAngle { get; set; } = 50f;
    [Property, Group("Animation")]
    public float MaxSpeedForRotatedModel { get; set; } = 10f;
    [Property, Group("Animation")]
    public float ModelRotationSpeed { get; set; } = 5f;


    private Vector3 _wishVelocity;
    private bool _isCrouching;
    private bool _isSprinting;


    protected override void OnUpdate()
    {
        _isSprinting = !_isCrouching && Input.Down("Run");

        Animate();
    }

    protected override void OnFixedUpdate()
    {
        HandleCrouching();
        _isCrouching = !CharacterController.Height.AlmostEqual(StandingHeight, (StandingHeight - CrouchingHeight) / 5f);

        BuildWishVelocty();

        if(Input.Pressed("Jump"))
            Jump();

        RotateBody();
        Move();
    }

    private void HandleCrouching()
    {
        var wantsCrouch = Input.Down("Duck");

        var currentHeight = Collider.Scale.z;
        var targetHeight = wantsCrouch ? CrouchingHeight : StandingHeight;
        var nextHeight = CharacterController.Height.LerpTo(targetHeight, HeightChangingSpeed / Time.Delta);

        if(!wantsCrouch && _isCrouching)
        {
            BBox bBox = BBox.FromPositionAndSize(Collider.Center + Vector3.Up * SkinSize / 2f, Collider.Scale + Vector3.Up * SkinSize);
            var traceResult = Scene.Trace.Box(bBox, Transform.Position, Transform.Position + Transform.Rotation.Up * (nextHeight - currentHeight))
                .WithCollisionRules("player")
                .IgnoreGameObject(GameObject)
                .Run();

            nextHeight = MathF.Min(nextHeight, currentHeight + traceResult.Distance);
        }

        if(nextHeight.AlmostEqual(currentHeight))
            return;

        CharacterController.Height = nextHeight;
        Collider.Scale = Collider.Scale.WithZ(nextHeight);
        Collider.Center = Collider.Center.WithZ(nextHeight / 2f);

        var targetEyeHeight = nextHeight + EyeHeightOffset;
        var nextEyeHeight = Eye.Transform.LocalPosition.z.LerpTo(targetEyeHeight, HeightChangingSpeed / Time.Delta);
        Eye.Transform.LocalPosition = Eye.Transform.LocalPosition.WithZ(nextEyeHeight);
    }

    private void BuildWishVelocty()
    {
        _wishVelocity = Vector3.Zero;

        var cameraRotation = Eye.Transform.Rotation;
        _wishVelocity = Input.AnalogMove * cameraRotation;

        _wishVelocity = Vector3.VectorPlaneProject(_wishVelocity, Transform.Rotation.Up);

        if(_wishVelocity.IsNearZeroLength)
            _wishVelocity = Vector3.Zero;
        else
            _wishVelocity = _wishVelocity.Normal;

        float speed;
        if(_isCrouching)
            speed = CrouchingSpeed;
        else if(_isSprinting)
            speed = RunningSpeed;
        else
            speed = WalkingSpeed;

        _wishVelocity *= speed;
    }

    private void Move()
    {
        var gravity = Scene.PhysicsWorld.Gravity;

        if(CharacterController.IsOnGround)
        {
            CharacterController.Velocity = Vector3.VectorPlaneProject(CharacterController.Velocity, Transform.Rotation.Up);
            CharacterController.Accelerate(_wishVelocity);
            CharacterController.ApplyFriction(GroundFriction);
        }
        else
        {
            CharacterController.Velocity += gravity * Time.Delta * 0.5f;
            CharacterController.Accelerate(_wishVelocity.ClampLength(MaxForce));
            CharacterController.ApplyFriction(AirFriction);
        }

        CharacterController.Move();


        if(CharacterController.IsOnGround)
        {
            CharacterController.Velocity = Vector3.VectorPlaneProject(CharacterController.Velocity, Transform.Rotation.Up);
        }
        else
        {
            CharacterController.Velocity += gravity * Time.Delta * 0.5f;
        }
    }

    private void RotateBody()
    {
        if(!Model.IsValid())
            return;

        var targetAngle = new Angles(0f, Eye.Transform.Rotation.Yaw(), 0f).ToRotation();
        float rotationDifference = Model.Transform.Rotation.Distance(targetAngle);

        if(rotationDifference > MaxModelDeltaAngle || CharacterController.Velocity.Length > MaxSpeedForRotatedModel)
            Model.Transform.Rotation = Rotation.Lerp(Model.Transform.Rotation, targetAngle, Time.Delta * ModelRotationSpeed);
    }

    private void Jump()
    {
        if(!CharacterController.IsOnGround)
            return;

        var upDirection = -Scene.PhysicsWorld.Gravity.Normal;
        if(upDirection.AlmostEqual(Vector3.Zero))
            upDirection = Transform.Rotation.Up;

        CharacterController.Punch(upDirection * JumpForce);
        AnimationHelper?.TriggerJump();
    }

    private void Animate()
    {
        if(!AnimationHelper.IsValid())
            return;

        AnimationHelper.WithWishVelocity(_wishVelocity);
        AnimationHelper.WithVelocity(CharacterController.Velocity);
        AnimationHelper.AimAngle = Eye.Transform.Rotation;
        AnimationHelper.IsGrounded = CharacterController.IsOnGround;
        AnimationHelper.WithLook(Eye.Transform.Rotation.Forward, 1f, 0.75f, 0.5f);
        AnimationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Auto;
        AnimationHelper.DuckLevel = 1f - (CharacterController.Height - CrouchingHeight) / (StandingHeight - CrouchingHeight);
    }
}
