using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrapAssigner : MonoBehaviour
{
    public CrapSetter Setter;
    public CrapPart Part;

	// Use this for initialization
	void Start ()
    {
        object exp = (Action<float>)(x => Part.MyValue = x);
        Setter.Activate.AddListener(x => Part.MyValue = x);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
