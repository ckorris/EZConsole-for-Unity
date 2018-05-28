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
    private Material _hoverMaterial;
    private Material[] _startMaterials;

    public virtual void Start()
    {
        //Set up materials for hovering
        _hoverMaterial = Resources.Load("HoveredMat") as Material;
        _startMaterials = gameObject.GetComponent<MeshRenderer>().materials;
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public bool CheckValidUseType(UseTypes use)
    {
        if (use == UseTypes.Grab || use == UseTypes.Press) return true;
        else return false; 
    }

    public virtual void GrabStart(VRControllerComponent controller)
    {
        throw new System.NotImplementedException();
    }

    public virtual void GrabEnd()
    {
        throw new System.NotImplementedException();
    }

    public virtual void PressStart(VRControllerComponent controller)
    {
        throw new System.NotImplementedException();
    }

    public virtual void PressEnd()
    {
        throw new System.NotImplementedException();
    }

    public virtual void HoverEnter()
    {
        ApplyHoverMaterials();
    }

    public virtual void HoverExit()
    {
        RevertMaterials();
    }

    public void ApplyHoverMaterials()
    {
        MeshRenderer rend = GetComponent<MeshRenderer>();
        Material[] hovermats = new Material[rend.materials.Length];
        for (int i = 0; i < hovermats.Length; i++)
        {
            hovermats[i] = _hoverMaterial;
        }

        rend.materials = hovermats;
    }
    public void RevertMaterials()
    {
        gameObject.GetComponent<MeshRenderer>().materials = _startMaterials;
    }
}
