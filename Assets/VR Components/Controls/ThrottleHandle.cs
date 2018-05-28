using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrottleHandle : BaseGrabAndPressControl<float>
{
    // Use this for initialization
    public override void Start ()
    {
        base.Start();
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
