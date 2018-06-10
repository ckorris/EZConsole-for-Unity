using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GrabOrPressControlBase : MonoBehaviour, IVRGrabbable, IVRPressable
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

    }

    public virtual void GrabEnd()
    {

    }

    public virtual void PressStart(VRControllerComponent controller)
    {

    }

    public virtual void PressEnd()
    {

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
