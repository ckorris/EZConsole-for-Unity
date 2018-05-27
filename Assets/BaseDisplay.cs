using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All display types inherit from this, but explicitly mention the type. 
/// Example: "BarDisplay : BaseDisplay<float>"
/// </summary>
/// <typeparam name="T"></typeparam>
[Serializable]
public class BaseDisplay<T> : BaseDisplay
{
    /// <summary>
    /// The value that gets retrieved from the Console's component. 
    /// </summary>
    public T DisplayValue;

    protected Dictionary<Component, List<Func<T>>> _delegateDictionary = new Dictionary<Component, List<Func<T>>>();

    /// <summary>
    /// Called by Console to assign a getter as this class's delegate to get called on UpdateValue. 
    /// </summary>
    /// <param name="del"></param>
    public virtual void RegisterDisplayDelegate(Component target, Func<T> del)
    {
        //Make sure there's an entry in the dictionary for the target component
        if (!_delegateDictionary.ContainsKey(target))
        {
            _delegateDictionary.Add(target, new List<Func<T>>());
        }

        //Add the new function to the list for that component. Sound the alarm if you're doing it twice. 
        if (!_delegateDictionary[target].Contains(del))
        {
            _delegateDictionary[target].Add(del);
        }
        else
        {
            Debug.LogError("Tried to register a function to " + name + " that already existed for that target component.", this);
        }
    }

    /// <summary>
    /// Call whenever you want to retrieve the value, then override with your own logic. This can go in Update() if you want but it doesn't have to. 
    /// Note that, to support multiple inputs, it will update "DisplayValue" once for each bound delegate. If there's more than one, it'll be overridden in an unpredictable order.
    /// This is not a problem if the inheriting display is designed for one value, so long as it is proplerly assigned to only one. 
    /// </summary>
    /// <param name="value"></param>
    public virtual void UpdateValue()
    {
        //Iterate through all bound components
        foreach (List<Func<T>> list in _delegateDictionary.Values)
        {
            //Iterate through all functions you will use to get a value
            foreach (Func<T> del in list)
            {
                DisplayValue = del.Invoke();
            }
        }
    }

    /// <summary>
    /// Removes a specific delegate from the list. 
    /// Checks all bound scripts for reference to the delegate, though a duplicate is probably unintentional. Specify the component to save performance. 
    /// </summary>
    /// <param name="del"></param>
    public void Deregister(Func<T> del)
    {
        //Go through every registered list to find the delegate references.
        foreach(List<Func<T>> list in _delegateDictionary.Values)
        {
            if(list.Contains(del))
            {
                list.Remove(del); //TODO: Make sure this is allowed, since we're modifying the element in a foreach. 
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

/// <summary>
/// Exists so we can store BaseDisplays without knowing their type. 
/// It's ridiculous that this works. 
/// </summary>
public abstract class BaseDisplay : MonoBehaviour { }
