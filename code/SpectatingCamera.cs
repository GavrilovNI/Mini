using Mini.Interfaces;
using Mini.Players;
using Sandbox;
using System.Linq;
using System.Threading.Tasks;

namespace Mini;

public class SpectatingCamera : Component
{
    public static SpectatingCamera? Instance { get; private set; }

    [Property]
    public bool SetTargetToPlayerOnStart { get; set; } = false;
    [Property, HideIf(nameof(SetTargetToPlayerOnStart), true)]
    public GameObject? TargetObject { get; private set; }
    public GameObject? Target { get; private set; }

    [Property, RequireComponent]
    public PlayerCameraController CameraController { get; set; } = null!;

    [Property]
    public float Speed { get; set; } = 10f;
    [Property]
    public float FastSpeed { get; set; } = 50f;


    private GameObject? _lastTargetObject;

    protected override Task OnLoad()
    {
        if(Scene.IsEditor)
            return Task.CompletedTask;

        if(Instance.IsValid())
        {
            GameObject.Destroy();
            return Task.CompletedTask;
        }
        Instance = this;
        return Task.CompletedTask;
    }


    protected override void OnAwake()
    {
        if(SetTargetToPlayerOnStart)
        {
            TargetObject = null;
            Target = null;
            SetTargetToPlayer();
        }
        else if(TargetObject is not null)
        {
            var targetObject = TargetObject;
            TargetObject = null;
            Target = null;
            SetTarget(targetObject);
        }

    }

    public void SetTarget(GameObject? target)
    {
        _lastTargetObject = TargetObject;

        CameraController.VisibilityController = null;
        var visibilityController = _lastTargetObject?.Components.Get<PlayerVisibilityController>(FindMode.EverythingInSelfAndDescendants);
        if(visibilityController.IsValid() && CameraController == visibilityController.CameraControllerOverride)
        {
            visibilityController.CameraControllerOverride = null;
            visibilityController.UpdateVisibility();
        }

        if(target.IsValid())
        {
            var eyeProvider = target.Components.Get<IEyeProvider>();
            if(eyeProvider is null)
            {
                TargetObject = target;
                Target = target;
            }
            else
            {
                TargetObject = target;
                Target = eyeProvider.Eye;
            }

            CameraController.SetView(false);
        }
        else
        {
            TargetObject = null;
            Target = null;
            Transform.Position = CameraController.Camera.Transform.Position;
            CameraController.SetView(true);
        }


        if(TargetObject.IsValid())
        {
            visibilityController = TargetObject?.Components.Get<PlayerVisibilityController>(FindMode.EverythingInSelfAndDescendants);

            if(visibilityController.IsValid())
            {
                visibilityController.CameraControllerOverride = CameraController;
                CameraController.VisibilityController = visibilityController;
                visibilityController.UpdateVisibility();
            }
        }

        CameraController.AllowChangingView = Target.IsValid();
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
        if(TargetObject.IsValid())
        {
            SetTarget(TargetObject);
        }
        else
        {
            SetTarget(null);
            Transform.Position = CameraController.Camera.Transform.Position;
            CameraController.SetView(true);
        }
    }

    protected virtual void SetTargetToPlayer()
    {
        SetTarget(Scene.Components.Get<Player>(FindMode.EnabledInSelfAndDescendants)?.GameObject);
    }

    protected virtual void UpdateTarget()
    {
        if(Target is not null && !Target.IsValid())
            OnLostTarget();

        if(Input.Pressed("Jump"))
        {
            if(Target is null)
            {
                if(_lastTargetObject.IsValid())
                    SetTarget(_lastTargetObject);
                else
                    SetTargetToPlayer();
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
                var players = Scene.GetAllComponents<Player>().Select(p => p.GameObject);
                if(!players.Any())
                {
                    SetTarget(null);
                }
                else
                {
                    var prevPlayer = players.Last();
                    bool found = false;

                    foreach(var player in players.Append(players.First()))
                    {
                        if(found)
                        {
                            SetTarget(prevPlayer);
                            break;
                        }

                        if(player == TargetObject)
                        {
                            if(moveNext)
                            {
                                found = true;
                            }
                            else
                            {
                                SetTarget(prevPlayer);
                                break;
                            }
                        }

                        prevPlayer = player;
                    }
                }
            }
        }
    }
}
