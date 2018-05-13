using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class ConsoleDisplay : MonoBehaviour
{
    public abstract Type DisplayType
    {
        get;
    }

    /// <summary>
    /// The method you call to update the inherited class's value. All it does it make sure it's type-safe before updating the value internally. 
    /// </summary>
    /// <param name="newvalue"></param>
    public void UpdateValue(object newvalue) //This is called externally, which calls the internal, so we can force it to be type-safe even after inheritance
    {
        //Make sure it's the correct type
        if(newvalue.GetType() != DisplayType)
        {
            //Put hard assets here later
            print("Tried to pass an invalid type to " + gameObject.name + "! Bad!");
        }
        else
        {
            InternalUpdateValue(newvalue);
        }
    }

    /// <summary>
    /// The method that will actually update the value. Protected so that you're forced to push it through the type-checking version. This way we can keep it generic yet safe. 
    /// </summary>
    /// <param name="newvalue"></param>
    protected abstract void InternalUpdateValue(object newvalue); 
}
