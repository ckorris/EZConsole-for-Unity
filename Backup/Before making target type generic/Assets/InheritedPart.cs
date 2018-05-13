using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InheritedPart : SomePart
{
    public bool InheritedBoolValue = true;

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

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
