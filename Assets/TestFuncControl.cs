using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestFuncControl : MonoBehaviour
{
    public Action OneShotAction;

    public Action<bool> BoolAction;

    public Func<bool, bool> ChangeBoolFunc;

	// Use this for initialization
	void Start ()
    {
        BoolAction += PrintBool;
        BoolAction += PrintOppositeBool;

        //PrintBoolAction.Invoke(true);

        ChangeBoolFunc += ChangeBool;

        //print(ChangeBoolFunc.Invoke(true));


	}
	
	// Update is called once per frame
	void Update ()
    {
		if(Input.GetKeyDown(KeyCode.Space))
        {
            if(OneShotAction != null) OneShotAction.Invoke();
        }
	}

    public void PrintBool(bool state)
    {
        print(state);
    }

    public void PrintOppositeBool(bool state)
    {
        print(!state);
    }

    public bool ChangeBool(bool state)
    {
        return !state;
    }
}
