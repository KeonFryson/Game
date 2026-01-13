using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
public class ConditionalHideAttribute : PropertyAttribute
{
    public string ConditionalSourceField { get; private set; }
    public bool HideInInspector { get; private set; }

    public ConditionalHideAttribute(string conditionalSourceField)
    {
        ConditionalSourceField = conditionalSourceField;
        HideInInspector = false;
    }

    public ConditionalHideAttribute(string conditionalSourceField, bool hideInInspector)
    {
        ConditionalSourceField = conditionalSourceField;
        HideInInspector = hideInInspector;
    }
}