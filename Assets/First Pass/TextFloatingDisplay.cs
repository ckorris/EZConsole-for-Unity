using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextFloatingDisplay : FloatConsoleDisplay
{
    public TextMesh Text;

	// Use this for initialization
	void Start ()
    {
        
	}
	
	// Update is called once per frame
	void Update ()
    {
		
	}

    protected override void InternalUpdateValue(object newvalue)
    {
        base.InternalUpdateValue(newvalue);
        
        if(Text)
        {
            Text.text = DisplayValue.ToString();
        }
    }
}
