using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Vector3ConsoleDisplay : ConsoleDisplay
{
    public override Type DisplayType
    {
        get
        {
            return typeof(Vector3);
        }
    }

    protected Vector3 DisplayValue;

    // Use this for initialization
    void Start ()
    {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
		
	}

    protected override void InternalUpdateValue(object newvalue)
    {
        DisplayValue = (Vector3)newvalue;
    }

}
