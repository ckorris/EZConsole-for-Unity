using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrottleHandle : BaseGrabAndPressControl<float>
{
    public float ArticulationLength = 0.3f;

    Vector3 _startPosition; //Local, set in Start();

    float _articulatePercentage
    {
        get
        {
            Vector3 forwardpos = Quaternion.Inverse(transform.localRotation) * (transform.localPosition - _startPosition);
            return forwardpos.z / ArticulationLength;
        }
        set
        {
            //Useful for clamping it
            Vector3 forwardpos = Vector3.forward * value * ArticulationLength;
            forwardpos = transform.localRotation * forwardpos;
            transform.localPosition = _startPosition + forwardpos;
        }
    }

    VRControllerComponent _controller; //The hand that's grabbing it, if any. May want to move this into a base class. 
    Vector3 _grabPos; //The position of the slider when the push/grab started
    Vector3 _controllerGrabPos; //The position of the controller when the push/grab started

    bool _isBeingUsed
    {
        get
        {
            return (_controller != null);
        }
    }

    // Use this for initialization
    public override void Start()
    {
        base.Start();

        _startPosition = transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        //Update the position if the controller is manipulating it.
        if (_isBeingUsed)
        {
            float _lastsetting = _articulatePercentage; //Cache this so we can see if we've changed it this frame

            Vector3 posdifference = _controller.transform.position - _controllerGrabPos; //The vector that the controller moved since start
            posdifference = transform.rotation * posdifference; //Rotate it so that the Z value is lined up with the sliding direction
            transform.position = _grabPos + transform.rotation * (Vector3.forward * posdifference.z); //Set the position relative to the grab start. 

            _articulatePercentage = Mathf.Clamp01(_articulatePercentage); //Don't let it exceed its bounds 

            //Activate the ability 
            if (_articulatePercentage != _lastsetting) //Don't bother calling if we haven't changed it
            {
                ActivateAction(_articulatePercentage);
            }
        }
    }

    public override void PressStart(VRControllerComponent controller)
    {
        base.PressStart(controller);

        _controller = controller;
        _grabPos = transform.position;
        _controllerGrabPos = _controller.transform.position;
    }

    public override void PressEnd()
    {
        base.PressEnd();

        _controller = null;
    }

    private void OnDrawGizmosSelected()
    {
        //Draw the blue line to show the sliding path.
        Vector3 zeropos = (Application.isPlaying) ? _startPosition : transform.position;
        Vector3 fullpos = zeropos + transform.localRotation * new Vector3(0, 0, ArticulationLength);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(zeropos, fullpos);
    }

    public override void GrabStart(VRControllerComponent controller)
    {
        PressStart(controller);
    }

    public override void GrabEnd()
    {
        PressEnd();
    }
}
