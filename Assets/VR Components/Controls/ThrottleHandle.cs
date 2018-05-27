using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrottleHandle : BaseGrabControl<float>
{
    // Use this for initialization
    void Start ()
    {
		
	}

    // Update is called once per frame
    void Update ()
    {
		
	}

    public override void GrabStart(VRControllerComponent controller)
    {
        base.GrabStart(controller);
        print("Grabbed throttle!");
    }

    public override void GrabEnd()
    {
        base.GrabEnd();
        print("Ungrabbed throttle!");
    }
}
