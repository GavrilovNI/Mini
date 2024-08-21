﻿using Mini.Players;
using Sandbox;
using System.Linq;
using static Sandbox.PhysicsContact;

namespace Mini;

public class SpectatingCamera : Component
{
    [Property]
    public GameObject? Target { get; private set; }

    [Property]
    public bool FindFirstTarget { get; set; } = true;

    [Property, RequireComponent]
    public PlayerCameraController CameraController { get; set; } = null!;

    [Property]
    public float Speed { get; set; } = 10f;
    [Property]
    public float FastSpeed { get; set; } = 50f;


    private GameObject? _lastTarget;
    private bool _firstTargetFound = false;


    protected override void OnAwake()
    {
        CameraController.IsFirstPerson = Target.IsValid();
    }

    public void SetTarget(GameObject? target)
    {
        _lastTarget = Target;
        Target = target;
        if(target is null)
            Transform.Position = CameraController.Camera.Transform.Position;
        CameraController.IsFirstPerson = target is null;
        _firstTargetFound = true;
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

        if(FindFirstTarget && !_firstTargetFound)
        {
            var players = Scene.Components.GetAll<Player>(FindMode.EnabledInSelfAndDescendants);

            var target = players.Where(p => p.Network.OwnerConnection == Connection.Local).FirstOrDefault()?.Eye;
            if(!target.IsValid())
                target = players.Where(p => p.Network.OwnerConnection != Connection.Local).FirstOrDefault()?.Eye;

            if(target.IsValid())
                SetTarget(target);
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
