using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRPilotHands : VRControllerComponent
{

#if HYPERFUSION //Not an actual directive, just want to let the code base compile. 
    public Vector3 PressPoint = new Vector3(0f, -0.0537f, 0.0204f);

    public bool HideHandsWhenGrabbing = true;

    List<IPressable> _CollidingPressableList = new List<IPressable>();
    List<IGrabbable> _CollidingGrabbableList = new List<IGrabbable>();

    IPressable _nearestPressable;
    IGrabbable _nearestGrabbable;

    IGrabbable _grabbedObject;

    GameObject RenderModelObject; //The "Model" object parented to the controllers that shows the controller. Used to hide it without hiding other children of the controller. 

    VRPointer _pointer; //We cache this so we can disable/enable it when we grab/ungrab stuff. 

    bool IsHoldingSomething
    {
        get
        {
            return (_grabbedObject != null);
        }
    }

    // Use this for initialization
    private void Start()
    {
        SteamVR_RenderModel rendermodel = GetComponentInChildren<SteamVR_RenderModel>();
        RenderModelObject = rendermodel.gameObject;
        _pointer = GetComponentInChildren<VRPointer>();
    }

    // Update is called once per frame
    private void Update()
    {
    #region Checking nearest controls

        //Know the closest pressables
        float nearestpressdistance = Mathf.Infinity;
        IPressable newNearestPressable = null;
        foreach (IPressable press in _CollidingPressableList)
        {
            float distance = Vector3.Distance(transform.position + PressPoint, press.GetGameObject().transform.position);
            if (distance < nearestpressdistance)
            {
                nearestpressdistance = distance;
                newNearestPressable = press;
            }
        }

        //if it's not the same object as last time, do stuff. 
        if (newNearestPressable != _nearestPressable)
        {
            if (_nearestPressable != null) _nearestPressable.UnHovered();

            _nearestPressable = newNearestPressable;

            if (_nearestPressable != null) _nearestPressable.Hovered();
        }

        //Know the nearest grabbables
        float nearestgrabdistance = Mathf.Infinity;
        IGrabbable newNearestGrabbable = null;
        foreach (IGrabbable grab in _CollidingGrabbableList)
        {
            if (grab == _grabbedObject) continue;

            float distance = Vector3.Distance(transform.position + GetComponent<CapsuleCollider>().center, grab.GetGameObject().transform.position);
            if (distance < nearestgrabdistance)
            {
                nearestgrabdistance = distance;
                newNearestGrabbable = grab;
            }
        }

        //if it's not the same object as last time, do stuff. 
        if (newNearestGrabbable != _nearestGrabbable)
        {
            if (_nearestGrabbable != null) _nearestGrabbable.UnHovered();

            _nearestGrabbable = newNearestGrabbable;

            if (_nearestGrabbable != null) _nearestGrabbable.Hovered();
        }

    #endregion

    #region Buttons
        //TODO: Reorganize all these now that we know what all we need. 
        //Check the trigger
        //First, did it go down? 
        if (ThisController.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
        {
            if (IsHoldingSomething)
            {
                _grabbedObject.Triggered(ButtonEventType.Pressed);
            }
            else
            {
                if (_nearestPressable != null)
                {
                    _nearestPressable.Pressed();
                }
            }
        }

        //Then just check if it's down. 
        if (ThisController.GetPress(SteamVR_Controller.ButtonMask.Trigger))
        {
            if (IsHoldingSomething)
            {
                _grabbedObject.Triggered(ButtonEventType.IsDown);
            }
            else
            {
                if (_nearestPressable != null)
                {
                    _nearestPressable.Pressed();
                }
            }
        }

        //Then check if it was released
        if (ThisController.GetPressUp(SteamVR_Controller.ButtonMask.Trigger))
        {
            if (IsHoldingSomething)
            {
                _grabbedObject.Triggered(ButtonEventType.Released);
            }
        }

        //Checking the touchpad. As of now we don't need the trackpad when not holding stuff, so just report it only when holding, and make it straightforward. 
        if(ThisController.GetTouchDown(SteamVR_Controller.ButtonMask.Touchpad) && IsHoldingSomething)
            _grabbedObject.TouchPadTouched(ButtonEventType.Pressed, ThisController.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0));
        if (ThisController.GetTouch(SteamVR_Controller.ButtonMask.Touchpad) && IsHoldingSomething)
            _grabbedObject.TouchPadTouched(ButtonEventType.IsDown, ThisController.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0));
        if (ThisController.GetTouchUp(SteamVR_Controller.ButtonMask.Touchpad) && IsHoldingSomething)
            _grabbedObject.TouchPadTouched(ButtonEventType.Released, ThisController.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0));


        //Checking the grip buttons
        if (ThisController.GetPressDown(SteamVR_Controller.ButtonMask.Grip))
        {
            //Ungrab something if you're already grabbing it. 
            if (_grabbedObject != null)
            {
                _grabbedObject.Ungrabbed(this);
                _grabbedObject = null;
                if (HideHandsWhenGrabbing) ChangeControllerVisibility(true);

                //Enable the VRPointer if we have one, since we disabled it when we grabbed the object. 
                if (_pointer != null)
                {
                    _pointer.enabled = true;
                }
            }
            else
            {
                //Start the grab.
                if (_nearestGrabbable != null) //If there's something within range, grab it directly. 
                {
                    _grabbedObject = _nearestGrabbable;
                    _nearestGrabbable.Grabbed(this, transform.position, transform.rotation);
                    if (HideHandsWhenGrabbing) ChangeControllerVisibility(false);

                    //Disable the VRPointer if we have one so we don't point at stuff. The VRPointer will automatically clean up any ongoing interactions. 
                    if (_pointer != null)
                    {
                        _pointer.enabled = false;
                    }
                }
            }
        }

    #endregion

        //Moving a grabbed object
        if (_grabbedObject != null)
        {
            _grabbedObject.Moved(transform.position, transform.rotation);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        GameObject go = other.gameObject;

        //Search to see if it's pressable
        IPressable pressable = go.GetComponent<IPressable>();
        if (pressable != null && !_CollidingPressableList.Contains(pressable)) _CollidingPressableList.Add(pressable);

        //And grabbable. 
        IGrabbable grabbable = go.GetComponent<IGrabbable>();
        if (grabbable != null && !_CollidingGrabbableList.Contains(grabbable)) _CollidingGrabbableList.Add(grabbable);
    }

    void OnTriggerExit(Collider other)
    {
        GameObject go = other.gameObject;

        //Deal with if it's pressable
        IPressable pressable = go.GetComponent<IPressable>();
        if (pressable != null && _CollidingPressableList.Contains(pressable)) _CollidingPressableList.Remove(pressable);

        //And grabbable. 
        IGrabbable grabbable = go.GetComponent<IGrabbable>();
        if (grabbable != null && _CollidingGrabbableList.Contains(grabbable)) _CollidingGrabbableList.Remove(grabbable);
    }

    void ChangeControllerVisibility(bool show)
    {
        /*MeshRenderer[] mrarray = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer mr in mrarray)
        {
            mr.enabled = show;
        }*/

        RenderModelObject.SetActive(show);


    }

    public void ForceGrab(IGrabbable grabbable)
    {
        //Ungrab whatever came before it 
        if (_grabbedObject != null)
        {
            _grabbedObject.Ungrabbed(this);
        }

        grabbable.Grabbed(this, transform.position, transform.rotation);
        _grabbedObject = grabbable;
        if (HideHandsWhenGrabbing) ChangeControllerVisibility(false);

        //Disable the VRPointer if we have one so we don't point at stuff. The VRPointer will automatically clean up any ongoing interactions. 
        if (_pointer != null)
        {
            _pointer.enabled = false;
        }
    }

    public void ForceUngrab()
    {
        //Ungrab whatever it's holding. 
        if (_grabbedObject != null)
        {
            //_grabbedObject.Ungrabbed(this);  //Not sure if I need this. 
            _grabbedObject = null;
            if (HideHandsWhenGrabbing) ChangeControllerVisibility(true);

            //Enable the VRPointer if we have one, since we disabled it when we grabbed the object. 
            if (_pointer != null)
            {
                _pointer.enabled = false;
            }
        }
    }
#endif
}
