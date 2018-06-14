using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Updates a textmesh based on a value you can retrieve one of several ways. 
/// Override with the type of value you'll get as T - lierally no other logic needed. 
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class TextDisplay<T> : MonoBehaviour
{
    public Func<T> GetDisplayValue;

    public TextMesh Text;

    [Tooltip("Updates the value in Awake()")]
    public bool GetTextOnAwake = false;
    [Tooltip("Updates the value in Start()")]
    public bool GetTextOnStart = false;
    [Tooltip("Updates the value in Update() each frame")]
    public bool GetTextOnUpdate = true;
    [Tooltip("Updates the value in FixedUpdate() every .02 seconds (by project default)")]
    public bool GetTextOnFixedUpdate = false;

    protected virtual void Awake()
    {
        if(GetTextOnAwake)
        {
            UpdateDisplayValue();
        }
    }

    // Use this for initialization
    protected virtual void Start()
    {
        if(GetTextOnStart)
        {
            UpdateDisplayValue();
        }
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if(GetTextOnUpdate)
        {
            UpdateDisplayValue();
        }
    }

    protected virtual void FixedUpdate()
    {
        if(GetTextOnFixedUpdate)
        {
            UpdateDisplayValue();
        }
    }



    /// <summary>
    /// Invokes the Funk and updates the text with that value. 
    /// This one is public so you can subscribe it to a delegate if you want. 
    /// </summary>
    public virtual void UpdateDisplayValue()
    {
        if(GetDisplayValue != null)
        {
            T newvalue = GetDisplayValue.Invoke();
            UpdateText(newvalue);
        }
    }

    /// <summary>
    /// Converts the new value to a string and sets the TextMesh to that string. 
    /// Separate method so that we can easily cast the generic T into a string, and 
    /// objects deriving from this class don't need to implement a custom method. 
    /// </summary>
    /// <param name="value"></param>
    protected virtual void UpdateText(T value)
    {
        if (Text)
        {
            Text.text = value.ToString();
        }
    }
}

