using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Calls a method with a flipped version of its own bool when pressed, but sets its value to the return value
/// Intended for when you want to /try/ turning something on/off, but it can fail (like a light that may not have power)
/// </summary>
public class ButtonPadBoolReturn : ButtonPad
{
    public Func<bool, bool> OnPress; //Bindable to EZConsole

    public bool State;

    public override void InvokeAction() //Gets called in ButtonPad.Update() when pressed
    {
        if (OnPress != null)
        {
            State = OnPress.Invoke(!State);
        }
    }
}


