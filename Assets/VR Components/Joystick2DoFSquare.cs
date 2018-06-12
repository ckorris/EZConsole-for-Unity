using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Joystick2DoFSquare : GrabControlBase
{
    public Action<Vector2> OnSlide;

    public float LengthWidth = 0.1f; //The length and width of the square bounds the joystick can slide within. 

    Vector2 _slidePosition //Lerped between -1 and 1.
    {
        get
        {
            Vector2 flatvector = FlattenVector3(Quaternion.Inverse(transform.rotation) * (transform.localPosition - _startPosition));
            float scaledx = flatvector.x / LengthWidth;
            float scaledy = flatvector.y / LengthWidth;

            return new Vector2(scaledx, scaledy);
        }
        set
        {
            //float unlerpedx = Mathf.InverseLerp(0 - (LengthWidth / 2), LengthWidth / 2, value.x);
            //float unlerpedy = Mathf.InverseLerp(0 - (LengthWidth / 2), LengthWidth / 2, value.y);
            float unscaledx = value.x * LengthWidth;
            float unscaledy = value.y * LengthWidth;
            Vector3 unflatvector = UnflattenVector2(new Vector2(unscaledx, unscaledy));
            transform.localPosition = _startPosition + transform.localRotation * unflatvector;
        }
    }

    Vector3 _startPosition; //Local, set at start. 

    VRControllerComponent _controller; //The hand that's grabbing it, if any. May want to move this into a base class. 
    Vector3 _grabPos; //The position of the joystick when the grab started
    Vector3 _controllerGrabPos; //The position of the controller when the grab started

    bool _isBeingUsed
    {
        get
        {
            return (_controller != null);
        }
    }

    public float HapticSlideStrengthModifier = 2f; //How strong the vibration is when you move the joystick

    // Use this for initialization
    public override void Start()
    {
        base.Start();

        _startPosition = transform.localPosition;
	}
	
	// Update is called once per frame
	void Update ()
    {
        //Update the position if the controller is manipulating it.
        if(_isBeingUsed)
        {
            Vector2 lastsetting = _slidePosition; //Cache this so we can see if and how much we've changed it this frame

            Vector3 posdifference = _controller.transform.position - _controllerGrabPos; //The vector that the controller moved since start
            posdifference = transform.rotation * posdifference; //Rotate it so that the Z value is lined up with the sliding direction

            float flatscaledx = Mathf.Clamp(posdifference.x / LengthWidth, -1, 1);
            float flatscaledy = Mathf.Clamp(posdifference.z / LengthWidth, -1, 1);

            Vector2 flatscaledvector = new Vector2(flatscaledx, flatscaledy); //Caching instead of setting directly because _slidePosition's getter has soooome compute to it. 
            _slidePosition = flatscaledvector;

            if(flatscaledvector != lastsetting)
            {
                //Activate the ability
                if(OnSlide != null)
                {
                    OnSlide.Invoke(flatscaledvector);
                }

                //Play haptics based on how far you slid the slider
                float slidedist = (flatscaledvector - lastsetting).magnitude;
                StartCoroutine(_controller.VibrateOnce(Mathf.Clamp01(slidedist * HapticSlideStrengthModifier), Time.deltaTime));
            }
        }
    }

    public override void GrabStart(VRControllerComponent controller)
    {
        base.GrabStart(controller);

        _controller = controller;
        _grabPos = transform.position;
        _controllerGrabPos = _controller.transform.position;

        StartCoroutine(_controller.VibrateOnce(0.1f, 0.01f)); //Buzz the controller very slightly
    }

    public override void GrabEnd()
    {
        base.GrabEnd();

        _controller = null;
    }

    /// <summary>
    /// Shorthand for getting the X and Y relative to its rotation, since this class will do that a lot. 
    /// </summary>
    /// <param name="vector"></param>
    /// <returns></returns>
    private Vector2 FlattenVector3(Vector3 vector)
    {
        //Vector3 derotatedvector = Quaternion.Inverse(transform.rotation) * vector;
        //return new Vector2(derotatedvector.x, derotatedvector.z);
        return new Vector2(vector.x, vector.z);
    }

    private Vector3 UnflattenVector2(Vector2 vector)
    {
        //Vector3 unflattenedvector = new Vector3(vector.x, 0, vector.y);
        //return transform.rotation * unflattenedvector;
        return new Vector3(vector.x, 0, vector.y);
    }

    private void OnDrawGizmosSelected()
    {
        //Draw the green square to show the bounds of where you can slide it around.
        Vector3 zeropos;
        if (Application.isPlaying && transform.parent != null)
        {
            zeropos = transform.parent.TransformPoint(_startPosition);
        }
        else
        {
            zeropos = transform.position;
        }

        Vector3 frontleft = transform.rotation * new Vector3(0 - LengthWidth, 0, LengthWidth) + zeropos;
        Vector3 frontright = transform.rotation * new Vector3(LengthWidth, 0, LengthWidth) + zeropos;
        Vector3 backleft = transform.rotation * new Vector3(0 - LengthWidth, 0, 0 - LengthWidth) + zeropos;
        Vector3 backright = transform.rotation * new Vector3(LengthWidth, 0, 0 - LengthWidth) + zeropos;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(frontleft, frontright);
        Gizmos.DrawLine(frontright, backright);
        Gizmos.DrawLine(backright, backleft);
        Gizmos.DrawLine(backleft, frontleft);
    }
}
