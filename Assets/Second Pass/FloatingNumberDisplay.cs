using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingNumberDisplay : BaseDisplay<float>
{
    public TextMesh Text;

	// Use this for initialization
	void Start ()
    {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
        UpdateValue();
        Text.text = DisplayValue.ToString();
	}
}
