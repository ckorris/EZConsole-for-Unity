using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BoolConsoleControl : ConsoleControl
{
    public override Type ControlType
    {
        get
        {
            return typeof(bool);
        }
    }

    public Dictionary<PartConsole, List<Action<bool>>> ActionDictionary = new Dictionary<PartConsole, List<Action<bool>>>();

    public void RegisterAction(PartConsole console, Action<bool> action)
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
            ActionDictionary.Add(console, new List<Action<bool>>());
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

    protected virtual void ActivateBool(bool value)
    {
        //Go through every console and deploy every action attached to it
        foreach (List<Action<bool>> actionlist in ActionDictionary.Values)
        {
            foreach (Action<bool> act in actionlist)
            {
                act.Invoke(value);
            }
        }
    }
}
