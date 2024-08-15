using Sandbox;
using System;

namespace Mini.Exceptions;

public class ComponentNotFoundException : Exception
{
    public readonly GameObject GameObject;
    public readonly Type ComponentType;

    public ComponentNotFoundException(GameObject gameObject, Type componentType) : base($"Component {componentType.FullName} not found on {gameObject}")
    {
        if(!componentType.IsAssignableTo(typeof(Component)))
            throw new ArgumentException("Got wrong component type.", nameof(componentType));

        GameObject = gameObject;
        ComponentType = componentType;
    }
}
