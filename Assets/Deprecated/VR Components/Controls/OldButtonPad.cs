using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OldButtonPad : OldBasePressControl<bool> //Should be null but you can't do that in C#. Need to add support in EZConsole. 
{

	// Use this for initialization
	public override void Start ()
    {
        base.Start();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public override void PressStart(VRControllerComponent controller)
    {
        base.PressStart(controller);
    }

    public override void PressEnd()
    {
        base.PressEnd();
    }
}
