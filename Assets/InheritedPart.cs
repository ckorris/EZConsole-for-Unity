using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InheritedPart : SomePart
{
    public bool NonPropertyBoolValue = true;

    private bool _inheritedBoolProperty = true;
    public bool InheritedBoolProperty
    {
        get
        {
            return _inheritedBoolProperty;
        }
        set
        {
            _inheritedBoolProperty = value;
        }
    }

    private Vector3 _vector3Property = Vector3.up;
    public Vector3 Vector3Property
    {
        get
        {
            return _vector3Property;
        }
    }

    public void SomeOneShotMethod()
    {
        print("One shot method called");
    }

    public void SetSomeFloatMethod(float somefloat)
    {
        print("Method called - " + somefloat);
    }
}
