using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All display types inherit from this, but explicitly mention the type. 
/// Example: "BarDisplay : BaseDisplay<float>"
/// </summary>
/// <typeparam name="T"></typeparam>
public class BaseDisplay<T> : BaseDisplay
{
    public Type DisplayType
    {
        get
        {
            return typeof(T);
        }
    }

    /// <summary>
    /// The value that gets retrieved from the Console's component. 
    /// </summary>
    public T DisplayValue;

    protected Func<T> DisplayDelegate; //TODO: Turn this into a dictionary of lists, so we can look up delegates by console and remove them. 

    /// <summary>
    /// Called by Console to assign a getter as this class's delegate to get called on UpdateValue. 
    /// </summary>
    /// <param name="del"></param>
    public virtual void RegisterDisplayDelegate(Func<T> del)
    {
        DisplayDelegate = del;
    }

    /// <summary>
    /// Call whenever you want to retrieve the value, then override with your own logic. This can go in Update() if you want but it doesn't have to. 
    /// </summary>
    /// <param name="value"></param>
    public virtual void UpdateValue()
    {
        if (DisplayDelegate != null)
        {
            DisplayValue = DisplayDelegate.Invoke();
        }
    }

}

/// <summary>
/// Exists so we can store BaseDisplays without knowing their type. 
/// It's ridiculous that this works. 
/// </summary>
public abstract class BaseDisplay : MonoBehaviour { }
