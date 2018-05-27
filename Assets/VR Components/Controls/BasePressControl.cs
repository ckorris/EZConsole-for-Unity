using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Derive from to make a VR control to press that can interface with EZConsole
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class BasePressControl<T> : BaseControl<T>, IVRPressable
{

    public virtual void PressStart(VRControllerComponent controller)
    {
        
    }

    public virtual void PressEnd()
    {
        
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public bool CheckValidUseType(UseTypes use)
    {
        if (use == UseTypes.Press) return true;
        else return false;
    }

    public virtual void HoverEnter()
    {
        
    }

    public virtual void HoverExit()
    {
        
    }


}
