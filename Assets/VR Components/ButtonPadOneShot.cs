using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fires a method with no parameters when pressed.
/// </summary>
public class ButtonPadOneShot : ButtonPad
{
    public Action OnPress; //Bindable to EZConsole

    public override void InvokeAction() //Gets called in ButtonPad.Update() when pressed
    {
        if (OnPress != null) OnPress.Invoke();
    }

}
