using Sandbox.Internal;
using Sandbox;
using System;

namespace Mini.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class HideIfNotNullAttribute : ConditionalVisibilityAttribute
{
    public string PropertyName { get; set; }

    public HideIfNotNullAttribute(string propertyName)
    {
        PropertyName = propertyName;
    }

    public override bool TestCondition(object targetObject, TypeDescription typeDescription)
    {
        PropertyDescription property = typeDescription.GetProperty(PropertyName);
        if(property == null)
            return true;

        if(!property.CanRead)
            return true;

        object value = property.GetValue(targetObject);
        return value is null;
    }

    public override bool TestCondition(SerializedObject serializedObject)
    {
        if(serializedObject.TryGetProperty(PropertyName, out var property))
        {
            object value = property.GetValue((object)serializedObject);
            return value is not null && value is not SerializedObject;
        }

        GlobalSystemNamespace.Log.Warning($"HideIfNotNullAttribute: Couldn't find property '{PropertyName}' on {serializedObject.TypeName}");
        return true;
    }
}
