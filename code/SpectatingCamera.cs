using Mini.Players;
using Sandbox;
using System.Linq;

namespace Mini;

public class SpectatingCamera : Component
{
    public static SpectatingCamera? Instance { get; private set; }

    [Property]
    public GameObject? Target { get; private set; }

    [Property, RequireComponent]
    public PlayerCameraController CameraController { get; set; } = null!;

    [Property]
    public float Speed { get; set; } = 10f;
    [Property]
    public float FastSpeed { get; set; } = 50f;


    private GameObject? _lastTarget;


    protected override void OnAwake()
    {
        if(Instance.IsValid())
        {
            GameObject.Destroy();
            return;
        }

        if(Scene.IsEditor)
            return;

        CameraController.IsFirstPerson = Target.IsValid();
        Instance = this;
    }

    public void SetTarget(GameObject? target)
    {
        _lastTarget = Target;
        Target = target;
        if(target is null)
            Transform.Position = CameraController.Camera.Transform.Position;
        CameraController.IsFirstPerson = target is null;
    }

    protected override void OnUpdate()
    {
        UpdateTarget();
        Move();
    }

    protected virtual void Move()
    {
        if(Target.IsValid())
        {
            Transform.Position = Target.Transform.Position;
            return;
        }

        Vector3 delta = Input.AnalogMove.WithZ(0).Normal;

        var speed = Input.Down("run") ? FastSpeed : Speed;
        Transform.Position += Transform.Rotation * delta * speed;
    }

    protected virtual void OnLostTarget()
    {
        Transform.Position = CameraController.Camera.Transform.Position;
        CameraController.IsFirstPerson = true;
    }

    protected virtual void UpdateTarget()
    {
        if(Target is not null && !Target.IsValid())
        {
            SetTarget(null);
            OnLostTarget();
        }

        if(Input.Pressed("Jump"))
        {
            if(Target is null)
            {
                if(_lastTarget.IsValid())
                    SetTarget(_lastTarget);
                else
                    SetTarget(Scene.Components.Get<Player>(FindMode.EnabledInSelfAndDescendants)?.Eye);
            }
            else
            {
                SetTarget(null);
            }
        }
        else if(Target is not null)
        {
            bool moveNext = Input.Pressed("attack2");
            if(Input.Pressed("attack1") || moveNext)
            {
                var playerEyes = Scene.GetAllComponents<Player>().Select(p => p.Eye);
                if(!playerEyes.Any())
                {
                    SetTarget(null);
                }
                else
                {
                    var prevPlayerEye = playerEyes.Last();
                    bool found = false;

                    foreach(var player in playerEyes.Append(playerEyes.First()))
                    {
                        if(found)
                        {
                            SetTarget(prevPlayerEye);
                            break;
                        }

                        if(player == Target)
                        {
                            if(moveNext)
                            {
                                found = true;
                            }
                            else
                            {
                                SetTarget(prevPlayerEye);
                                break;
                            }
                        }

                        prevPlayerEye = player;
                    }
                }
            }
        }
    }
}
