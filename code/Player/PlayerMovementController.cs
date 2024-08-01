using Sandbox;
using Sandbox.Citizen;

namespace Mini.Player;

public sealed class PlayerMovementController : Component
{
    [RequireComponent]
    [Property]
    private CharacterController CharacterController { get; set; } = null!;

    [Property]
    private CitizenAnimationHelper? AnimationHelper { get; set; }
    [Property]
    private ModelRenderer? Model { get; set; }

    [Property]
    private GameObject Eye { get; set; } = null!;


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
        _isCrouching = Input.Down("Duck");
        _isSprinting = Input.Down("Run");

        Animate();
    }

    protected override void OnFixedUpdate()
    {
        BuildWishVelocty();

        if(Input.Pressed("Jump"))
            Jump();

        RotateBody();
        Move();
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
        AnimationHelper.DuckLevel = _isCrouching ? 1f : 0f;
    }
}
