using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestFloatControl : FloatConsoleControl
{

	// Use this for initialization
	void Start ()
    {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
		if(Input.GetKeyDown(KeyCode.Z))
        {
            //print(ActivateFloat.GetPersistentMethodName(0));
            ActivateFloat(Random.Range(0f, 10f));
        }
	}
}
