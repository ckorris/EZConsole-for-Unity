using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Vector3ConsoleControl : ConsoleControl
{
    public override Type ControlType
    {
        get
        {
            return typeof(bool);
        }
    }
    public Dictionary<PartConsole, List<Action<Vector3>>> ActionDictionary = new Dictionary<PartConsole, List<Action<Vector3>>>();

    public void RegisterAction(PartConsole console, Action<Vector3> action)
    {
        //TODO: Add hard assert once in Hyperfusion
        if (ActionDictionary.ContainsKey(console) && ActionDictionary[console].Contains(action))
        {
            print("Adding duplicate action to " + name + " from " + console.name + ".");
            return;
        }

        //If we don't have a list setup for that console, add one. 
        if (!ActionDictionary.ContainsKey(console))
        {
            ActionDictionary.Add(console, new List<Action<Vector3>>());
        }

        ActionDictionary[console].Add(action);
    }

    public override void DeregisterPart(PartConsole console)
    {
        if (ActionDictionary.ContainsKey(console))
        {
            ActionDictionary.Remove(console);
        }
    }

    protected virtual void ActivateVectorThree(Vector3 value)
    {
        //Go through every console and deploy every action attached to it
        foreach (List<Action<Vector3>> actionlist in ActionDictionary.Values)
        {
            foreach (Action<Vector3> act in actionlist)
            {
                act.Invoke(value);
            }
        }
    }

}
