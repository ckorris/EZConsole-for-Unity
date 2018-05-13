using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class FloatConsoleControl : ConsoleControl
{
    public override Type ControlType
    {
        get
        {
            return typeof(float);
        }
    }

    public Dictionary<PartConsole, List<Action<float>>> ActionDictionary = new Dictionary<PartConsole, List<Action<float>>>();

    //public FloatEvent ActivateFloat = new FloatEvent();

    public void RegisterAction(PartConsole console, Action<float> action)
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
            ActionDictionary.Add(console, new List<Action<float>>());
        }

        ActionDictionary[console].Add(action);
    }

    public override void DeregisterPart(PartConsole console)
    {
        if(ActionDictionary.ContainsKey(console))
        {
            ActionDictionary.Remove(console);
        }
    }

    protected virtual void ActivateFloat(float value)
    {
        //Go through every console and deploy every action attached to it
        foreach(List<Action<float>> actionlist in ActionDictionary.Values)
        {
            foreach(Action<float> act in actionlist)
            {
                act.Invoke(value);
            }
        }
    }

}
