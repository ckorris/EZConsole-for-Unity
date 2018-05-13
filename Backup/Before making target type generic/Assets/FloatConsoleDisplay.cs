using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class FloatConsoleDisplay : ConsoleDisplay
{
    public override Type DisplayType
    {
        get
        {
            return typeof(float);
        }
    }

    protected float DisplayValue;

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
        DisplayValue = (float)newvalue;
    }
}
