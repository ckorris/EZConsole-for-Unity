using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Flips its own bool value when pressed and fires a method with it. 
/// Intended for situations like a light switch where you turn something on/off.  
/// </summary>
public class ButtonPadBool : ButtonPad
{
    public Action<bool> OnPress; //Bindable to EZConsole

    public bool State = false;

    public override void InvokeAction() //Gets called in ButtonPad.Update() when pressed
    {
        State = !State;
        if (OnPress != null) OnPress.Invoke(State);
    }
}
