using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ButtonPad : PressControlBase
{
    //public Action OnPress; //Gets called when, well, you know. This is the delegate we bind to the Console. 

    public float ArticulationLength = 0.03f; //How far it extends downward
    float _articulatePercentage
    {
        get
        {
            Vector3 downpos = Quaternion.Inverse(transform.localRotation) * (transform.localPosition - _startPosition);
            return -downpos.y / ArticulationLength;
        }
        set
        {
            //Useful for clamping it
            Vector3 downpos = Vector3.down * value * ArticulationLength;
            downpos = transform.localRotation * downpos;
            transform.localPosition = _startPosition + downpos;
        }
    }

    public float SecondsToPopToStartPos = 0.25f;

    Vector3 _startPosition; //Local, set in Start()

    VRControllerComponent _controller; //The hand that's grabbing it, if any. 
    Vector3 _grabPos; //The position of the button when the push/grab started
    Vector3 _controllerGrabPos; //The position of the controller when the push/grab started

    AudioSource _audioSource;

    bool _isBeingUsed
    {
        get
        {
            return (_controller != null);
        }
    }

    public float _canPressOnceThreshold = 0.75f; //Articulation has to pass this AND hit the end to fire once. Prevents edging to spam.
    bool _crossedThresholdSinceLastFire = false; //True if we've passed the threshold but haven't fired yet. 

    // Use this for initialization
    public override void Start()
    {
        base.Start();

        _startPosition = transform.localPosition;
        _audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        //Update the position if the controller is manipulating it.
        if (_isBeingUsed)
        {
            float _lastsetting = _articulatePercentage; //Cache this so we can see how we've changed it this frame

            Vector3 posdifference = _controller.transform.position - _controllerGrabPos; //The vector that the controller moved since start
            posdifference = transform.rotation * posdifference; //Rotate it so that the Y value is lined up with the sliding direction
            transform.position = _grabPos + transform.rotation * (Vector3.up * posdifference.y); //Set the position relative to the grab start. 

            _articulatePercentage = Mathf.Clamp01(_articulatePercentage); //Don't let it exceed its bounds 

            if(_lastsetting < _canPressOnceThreshold && _articulatePercentage >= _canPressOnceThreshold) //Did we just cross the threshold?
            {
                _crossedThresholdSinceLastFire = true; //Let us fire the next time we hit 100%.
            }

            if(_articulatePercentage >= 1) //We're at the end. 
            {
                if(_crossedThresholdSinceLastFire) //We're allowed to fire once. 
                {
                    //if(OnPress != null) OnPress.Invoke(); //Fire the action //Except no, we're making this an abstract class.
                    InvokeAction(); //This method should call the action with whatever logic you'd prefer. 
                    _crossedThresholdSinceLastFire = false; //Prevent another invocation until the button returns past the threshold
                    if (_audioSource) _audioSource.Play(); //Play the boop or whatever
                    StartCoroutine(_controller.VibrateOnce(0.5f, 0.02f)); //Buzz the controller slightly
                }
            }
        }
        else
        {
            //Return the button to its top position
            if(_articulatePercentage > 0)
            {
                _articulatePercentage -= Time.deltaTime / SecondsToPopToStartPos;
                if (_articulatePercentage < 0) _articulatePercentage = 0;
            }
        }
    }

    public override void PressStart(VRControllerComponent controller)
    {
        base.PressStart(controller);

        _controller = controller;
        _grabPos = transform.position;
        _controllerGrabPos = _controller.transform.position;

        //Check the threshold now in case they hit the button while it was on the way up. 
        if (_articulatePercentage > _canPressOnceThreshold)
        {
            _crossedThresholdSinceLastFire = true;
        }

        StartCoroutine(_controller.VibrateOnce(0.1f, 0.01f)); //Buzz the controller very slightly
    }

    public override void PressEnd()
    {
        base.PressEnd();

        _controller = null;
    }

    /// <summary>
    /// InvokeAction is called when the button is pressed. Being abstract lets you override it
    /// with logic that can call any kind of action you want. 
    /// </summary>
    public abstract void InvokeAction();

}
