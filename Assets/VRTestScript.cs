using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRTestScript : MonoBehaviour
{
    bool _someBoolState = false;
    float _someFloatState = 0f;

    public int _someIntProperty = 55;
    public int SomeIntProperty
    {
        get
        {
            return _someIntProperty;
        }
        set
        {
            _someIntProperty = value;
        }
    }

    public Camera FrontCamera { get; private set; }

    public void ActivateOnce()
    {
        print("PEW PEW PEW");
    }

    public void SetSomeFloatValue(float newvalue)
    {
        _someFloatState = newvalue;
        print("Set float to " + newvalue);
    }

    public void SetBoolWithoutCallback(bool somebool)
    {
        _someBoolState = somebool;
        print("Set state to " + somebool);
    }

    public bool SetBoolWithCallback(bool somebool)
    {
        int diceroll = Random.Range(1, 3);
        if(diceroll == 1)
        {
            //Fail the change
            _someBoolState = !somebool;
            print("Tried to set _someBoolState but failed: value now equal to " + _someBoolState);
        }
        else
        {
            //Successfully changed
            _someBoolState = somebool;
            print("Successfully changed _someBoolState to " + somebool);
        }
        return _someBoolState;
    }


	// Use this for initialization
	void Awake ()
    {
        FrontCamera = GetComponentInChildren<Camera>();
	}
	
	// Update is called once per frame
	void Update ()
    {
		
	}
}
