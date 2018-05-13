using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeMethodTest : MonoBehaviour
{
    public Type testtype = typeof(string);
	// Use this for initialization
	void Start ()
    {
        typeof(MakeMethodTest).GetMethod("PrintTypeName").MakeGenericMethod(testtype).Invoke(this, null);
	}
	
	// Update is called once per frame
	void Update ()
    {
		
	}

    public void PrintTypeName<T>()
    {
        print(typeof(T).Name);
    }
}
