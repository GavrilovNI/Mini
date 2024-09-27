using Sandbox;
using static Sandbox.SceneModel;

namespace Mini.Players;

public class PlayerFootsteps : Component
{
    [Property]
    public SkinnedModelRenderer Model { get; set; } = null!;
    [Property]
    public PlayerMovementController MovementController { get; set; } = null!;
    [Property]
    public float MinTimeBetweenSteps { get; set; } = 0.2f;
    [Property]
    public float MaxGroundDistance { get; set; } = 20f;

    private TimeSince _timeSinceStep;


    protected override void OnEnabled()
    {
        Model.OnFootstepEvent += OnFootstep;
        MovementController.Jumped += OnJumped;
        MovementController.Grounded += OnGrounded;
    }

    protected override void OnDisabled()
    {
        Model.OnFootstepEvent -= OnFootstep;
        MovementController.Jumped -= OnJumped;
        MovementController.Grounded -= OnGrounded;
    }

    private void OnJumped(PlayerMovementController _)
    {
        var surface = FindSurface(Transform.Position);
        if(surface is null)
            return;

        var sound = surface.Sounds.FootLaunch;
        if(sound is null)
            return;

        Sound.Play(sound, Transform.Position);
    }

    private void OnGrounded(PlayerMovementController _)
    {
        var surface = FindSurface(Transform.Position);
        if(surface is null)
            return;

        var sound = surface.Sounds.FootLand;
        if(sound is null)
            return;

        Sound.Play(sound, Transform.Position);
    }

    private void OnFootstep(SceneModel.FootstepEvent footstepEvent)
    {
        if(_timeSinceStep < MinTimeBetweenSteps)
            return;

        var surface = FindSurface(footstepEvent.Transform.Position);
        if(surface is null)
            return;

        var sound = footstepEvent.FootId == 0 ? surface.Sounds.FootLeft : surface.Sounds.FootRight;
        if(sound is null)
            return;

        var handle = Sound.Play(sound, footstepEvent.Transform.Position);
        handle.Volume *= footstepEvent.Volume;
        _timeSinceStep = 0;
    }

    private Surface? FindSurface(Vector3 position)
    {
        var traceResult = Scene.Trace.Ray(position + Transform.Rotation.Up * 5, position + Transform.Rotation.Down * MaxGroundDistance)
            .WithCollisionRules("player")
            .Run();

        if(!traceResult.Hit)
            return null;

        return traceResult.Surface;
    }
}
