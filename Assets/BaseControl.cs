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

    //protected Action<T> ActionDelegate; //The delegate that gets called when you use the control, bound to a method on the console's target script. (TODO: turn to dictionary by console for removal)

    protected Dictionary<Component, List<Action<T>>> _delegateDictionary = new Dictionary<Component, List<Action<T>>>();

    /// <summary>
    /// Called by the console to assign this control to a method (which may be a setter) in the console's target script. 
    /// </summary>
    /// <param name="del"></param>
    public virtual void RegisterControlDelegate(Component target, Action<T> del) 
    {
        //ActionDelegate = del;
        //Make sure there's an entry in the dictionary for the target component
        if(!_delegateDictionary.ContainsKey(target))
        {
            _delegateDictionary.Add(target, new List<Action<T>>());
        }

        //Add the new action to the list for that component. Sound the alarm if you're doing it twice. 
        if (_delegateDictionary[target].Contains(del))
        {
            _delegateDictionary[target].Add(del);
        }
        else
        {
            Debug.LogError("Tried to register a function to " + name + " that already existed for that target component.", this);
        }
    }

    /// <summary>
    /// Call when you use the control. 
    /// </summary>
    /// <param name="value"></param>
    public virtual void ActivateAction(T value)
    {
        /*if(ActionDelegate != null)
        {
            ActionDelegate.Invoke(value);
        }*/

        //Iterate through all bound components
        foreach(List<Action<T>> list in _delegateDictionary.Values)
        {
            //Iterate through all actions you will call
            foreach(Action<T> act in list)
            {
                act.Invoke(value);
            }
        }
    }

    /// <summary>
    /// Removes a specific delegate from the list. 
    /// Checks all bound scripts for reference to the delegate, though a duplicate is probably unintentional. Specify the component to save performance. 
    /// </summary>
    public void Deregister(Action<T> act)
    {
        //Go through every registered list to find the delegate references.
        foreach (List<Action<T>> list in _delegateDictionary.Values)
        {
            if (list.Contains(act))
            {
                list.Remove(act); //TODO: Make sure this is allowed, since we're modifying the element in a foreach. 
            }
        }
    }

    /// <summary>
    /// Removes all delegates associated with a component. 
    /// </summary>
    /// <param name="component"></param>
    public void Deregister(Component component)
    {
        _delegateDictionary.Remove(component);
    }
}

public abstract class BaseControl : MonoBehaviour { }
