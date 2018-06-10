﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Derive from to make a VR control to grab that can interface with EZConsole
/// </summary>
/// <typeparam name="T"></typeparam>
/// 
public abstract class OldBaseGrabControl<T> : BaseControl<T>, IVRGrabbable
{
    private Material _hoverMaterial;
    private Material[] _startMaterials;

    public virtual void Start()
    {
        //Set up materials for hovering
        _hoverMaterial = Resources.Load("HoveredMat") as Material;
        _startMaterials = gameObject.GetComponent<MeshRenderer>().materials;
    }

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