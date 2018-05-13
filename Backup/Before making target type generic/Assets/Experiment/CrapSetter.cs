using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrapSetter : MonoBehaviour
{
    public FloatEvent Activate = new FloatEvent();

	// Use this for initialization
	void Start ()
    {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
		if(Input.GetKeyDown(KeyCode.Space))
        {
            Activate.Invoke(Random.Range(1f, 10f));
        }
	}
}
