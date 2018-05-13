using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseControl<T> : BaseControl
{
    public Type ControlType
    {
        get
        {
            return typeof(T);
        }
    }

    protected Action<T> ActionDelegate; //The delegate that gets called when you use the control, bound to a method on the console's target script. (TODO: turn to dictionary by console for removal)

    /// <summary>
    /// Called by the console to assign this control to a method (which may be a setter) in the console's target script. 
    /// </summary>
    /// <param name="del"></param>
    public virtual void RegisterActionDelegate(Action<T> del) 
    {
        ActionDelegate = del;
    }

    /// <summary>
    /// Call when you use the control. 
    /// </summary>
    /// <param name="value"></param>
    public virtual void ActivateAction(T value)
    {
        if(ActionDelegate != null)
        {
            ActionDelegate.Invoke(value);
        }
    }
}

public abstract class BaseControl : MonoBehaviour { }
