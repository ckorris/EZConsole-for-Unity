using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Derive from to make a VR control to grab that can interface with EZConsole
/// </summary>
/// <typeparam name="T"></typeparam>
/// 
public abstract class BaseGrabControl<T> : BaseControl<T>, IVRGrabbable
{
    public virtual void GrabStart(VRControllerComponent controller)
    {
        
    }

    public virtual void GrabEnd()
    {
        
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public bool CheckValidUseType(UseTypes use)
    {
        if (use == UseTypes.Grab) return true;
        else return false;
    }

    public virtual void HoverEnter()
    {
        
    }

    public virtual void HoverExit()
    {
        
    }


}
