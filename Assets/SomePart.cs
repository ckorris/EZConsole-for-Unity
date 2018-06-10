using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEditor.Events;

public class SomePart : MonoBehaviour
{
    public bool BoolValue;

    bool _boolProperty = true;
    public bool BoolProperty
    {
        get
        {
            return _boolProperty;
        }
        set
        {
            _boolProperty = value;
        }
    }

    public float FloatValue;

    public float _floatProperty = 5.5f; //Only public for testing in the editor
    public float FloatProperty
    {
        get
        {
            return _floatProperty;
        }
        set
        {
            _floatProperty = value;
        }
    }

	// Use this for initialization
	void Start ()
    {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
		
	}

    public void Fire()
    {
        print("Pew");
    }


}
