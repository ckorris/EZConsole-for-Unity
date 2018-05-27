using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class VRHand : VRControllerComponent
{

    public List<IVRUsable> _collidingUsables = new List<IVRUsable>(); //All usable objects touching the collider

    IVRUsable _currentUsable; //The thing we're currently using, if we are. 
    IVRUsable _hoveringUsable; //The closest usable object touching the collider

    UseState _useState = UseState.Empty; //What's the hand doing right now? 



    // Use this for initialization
    void Start()
    {
        //Make sure the attached collider is a trigger
        Collider collider = GetComponent<Collider>();
        collider.isTrigger = true;

        //Make sure the rigidbody is kinematic
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

    }

    // Update is called once per frame
    void Update()
    {
        //What we do here depends on what the hand's doing. 
        if (_useState == UseState.Empty)
        {
            #region Update Hovered Usables
            //Update the hovered objects
            if (_collidingUsables.Count > 0)
            {
                float nearestdist = Mathf.Infinity;
                IVRUsable nearestusable = null; //Will be the new hovered object after the foreach loop. 

                foreach (IVRUsable usable in _collidingUsables)
                {
                    //Find out how close it is. Note that later it may be better to check from a different point than the controller's center. 
                    float dist = Vector3.Distance(transform.position, usable.GetGameObject().transform.position);

                    if (dist < nearestdist)
                    {
                        //It's the closest so far. Log it. 
                        nearestdist = dist;
                        nearestusable = usable;
                    }
                }

                //If the hovered object is different from the one last frame, handle that. 
                if (_hoveringUsable != nearestusable)
                {
                    //Clear the old hover, if there was one
                    if (_hoveringUsable != null)
                    {
                        _hoveringUsable.HoverExit();
                    }

                    nearestusable.HoverEnter();
                    _hoveringUsable = nearestusable;
                }

            }
            else
            {
                //If we were hovering over something last frame, remove the hover. 
                if (_hoveringUsable != null)
                {
                    _hoveringUsable.HoverExit();
                    _hoveringUsable = null;
                }
            }
            #endregion

            //Grip
            if (ThisController.GetPressDown(SteamVR_Controller.ButtonMask.Grip))
            {
                //Check if we have something to grab
                if (_hoveringUsable != null && _hoveringUsable.CheckValidUseType(UseTypes.Grab))
                {
                    //Grab the thing as a grabbable, not the base IVRUsable interface
                    IVRGrabbable grabbable = _hoveringUsable as IVRGrabbable;
                    grabbable.GrabStart(this);

                    _currentUsable = _hoveringUsable;

                    //Clear the hover effects, and the reference so that it will get re-hovered when we let go. 
                    _currentUsable.HoverExit();
                    _hoveringUsable = null;

                    _useState = UseState.Grabbing;
                }
            }

            //Press
            if (ThisController.GetPress(SteamVR_Controller.ButtonMask.Trigger))
            {
                //Check if we have something to press
                if (_hoveringUsable != null && _hoveringUsable.CheckValidUseType(UseTypes.Press))
                {
                    //Press the thing as a pressable, not the base IVRUsable interface
                    IVRPressable pressable = _hoveringUsable as IVRPressable;
                    pressable.PressStart(this);

                    _currentUsable = _hoveringUsable;

                    //Clear the hovering effects, and the reference so that it will get re-hovered when we let go. 
                    _currentUsable.HoverEnter();
                    _hoveringUsable = null;

                    _useState = UseState.Grabbing;

                }
            }


        }
        else if (_useState == UseState.Grabbing)
        {
            //Ungrip
            if (ThisController.GetPressDown(SteamVR_Controller.ButtonMask.Grip))
            {
                //Ungrab the thing
                IVRGrabbable grabbable = _currentUsable as IVRGrabbable;
                grabbable.GrabEnd();

                _currentUsable = null;
                _useState = UseState.Empty;

            }
        }
        else if (_useState == UseState.Pressing)
        {
            //Unpress
            if (!ThisController.GetPress(SteamVR_Controller.ButtonMask.Trigger) || //User let go of the controller
                !_collidingUsables.Contains(_currentUsable)) //User moved the controller too far away
            {
                //Unpress the thing
                IVRPressable pressable = _currentUsable as IVRPressable;
                pressable.PressEnd();

                _currentUsable = null;
                _useState = UseState.Empty;

            }
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        IVRUsable usable = other.GetComponent<IVRUsable>();
        if (usable == null)
        {
            return; //We don't care
        }

        //Make sure it's not already there
        if (!_collidingUsables.Contains(usable))
        {
            _collidingUsables.Add(usable);
        }
        else
        {
            //TODO: Add hardassert
            Debug.LogError("Tried to add " + usable + " to VRHand's colliding usables list but it was already there.");
        }

    }

    private void OnTriggerExit(Collider other)
    {
        IVRUsable usable = other.GetComponent<IVRUsable>();
        if (usable == null)
        {
            return; //We don't care
        }

        if (_collidingUsables.Contains(usable))
        {
            _collidingUsables.Remove(usable);
        }
        else
        {
            //TODO: Add hardassert
            Debug.LogError("Tried to remove " + usable + " from VRHAND's colliding usables list but it wasn't in it.");
        }
    }
}

public interface IVRUsable //Anything the VR hands can manipulate
{
    GameObject GetGameObject(); //Should be return this.gameObject

    bool CheckValidUseType(UseTypes use); //Return if it's a press, a grab or something else we add later (touch?) 

    void HoverEnter(); //When the hand passes onto the object but isn't using it

    void HoverExit(); //When the hand is no longer touching the object, or it started using it  

}

public interface IVRPressable : IVRUsable
{
    void PressStart(VRControllerComponent controller);

    void PressEnd(); //Should be called when the trigger is released or the hand is too far away
}

public interface IVRGrabbable : IVRUsable
{
    void GrabStart(VRControllerComponent controller);

    void GrabEnd();
}

public enum UseTypes //For IVRUsable to indicate what kind of object it is to VRHands
{
    Press,
    Grab
}

enum UseState //For knowing what the controller is currently doing
{
    Empty,
    Pressing,
    Grabbing
}
