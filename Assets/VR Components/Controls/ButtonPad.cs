using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonPad : BasePressControl<bool> //Should be null but you can't do that in C#. Need to add support in EZConsole. 
{

	// Use this for initialization
	void Start ()
    {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public override void PressStart(VRControllerComponent controller)
    {
        base.PressStart(controller);
        print("Press button!");
    }

    public override void PressEnd()
    {
        base.PressEnd();
        print("Unpress button!");
    }
}
