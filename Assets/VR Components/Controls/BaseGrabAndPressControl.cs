using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Derive from to make a VR control that you can both grab or press, and that interfaces with EZConsole
/// Example of this would be a throttle: You can slide it gently or hold on for awhile. 
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class BaseGrabAndPressControl<T> : BaseControl<T>, IVRGrabbable, IVRPressable
{
    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public bool CheckValidUseType(UseTypes use)
    {
        if (use == UseTypes.Grab || use == UseTypes.Press) return true;
        else return false; 
    }

    public void GrabEnd()
    {
        throw new System.NotImplementedException();
    }

    public void GrabStart(VRControllerComponent controller)
    {
        throw new System.NotImplementedException();
    }

    public void HoverEnter()
    {
        throw new System.NotImplementedException();
    }

    public void HoverExit()
    {
        throw new System.NotImplementedException();
    }

    public void PressEnd()
    {
        throw new System.NotImplementedException();
    }

    public void PressStart(VRControllerComponent controller)
    {
        throw new System.NotImplementedException();
    }
}
