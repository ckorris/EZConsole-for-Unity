using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BoolConsoleDisplay : ConsoleDisplay
{
    public override Type DisplayType
    {
        get
        {
            return typeof(bool);
        }
    }

    protected bool DisplayValue;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
		
	}

    protected override void InternalUpdateValue(object newvalue)
    {
        DisplayValue = (bool)newvalue;
    }
}
