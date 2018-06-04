using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// For testing purposes. Hit P to change the target value to ValueToSet. 
/// </summary>
public class FloatAssignControl : BaseControl<float>
{
    public float ValueToSet = 10.25f;

	// Use this for initialization
	void Start ()
    {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
		if(Input.GetKeyDown(KeyCode.P))
        {
            print("Setting value to " + ValueToSet);
            ActivateAction(ValueToSet);
        }
	}

}
