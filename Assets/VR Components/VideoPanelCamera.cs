using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws the view of a camera onto the MeshRenderer's material, without forcing the camera's Target Texture value to target our material. 
/// Designed with a plane mesh in mind. Works best if the material is unlit. 
/// </summary>
public class VideoPanelCamera : MonoBehaviour
{
    public Func<Camera> GetCamera; //Bindable delegate for EZConsole

    public Camera TargetCamera; //The camera we'll render to the panel. If GetCamera is bound, we'll overwrite this. 

    public bool CheckCameraReferenceEachFrame = false; //If true, each frame it'll update TargetCamera using GetCamera. 

    private MeshRenderer _renderer;
    private RenderTexture _renderTexture;

	// Use this for initialization
	void Start ()
    {
        //Call the delegate (if it's bound to something) to get the camera reference. 
		if(GetCamera != null)
        {
            TargetCamera = GetCamera.Invoke();
        }

        MatchCameraAspectRatio(); //Give the plane the same aspect ratio as the camera

        //Set up the renderer and rendertexture
        _renderTexture = new RenderTexture(TargetCamera.pixelWidth, TargetCamera.pixelHeight, 16); //I double we need the depth buffer but oh wells
        _renderer = GetComponent<MeshRenderer>();
        Material copymat = new Material(_renderer.material); //We instance the material so we don't modify it globally. 
        copymat.mainTexture = _renderTexture; //Now when we update the rendertexture it'll show on this material. 
        _renderer.material = copymat; 

    }

    // Update is called once per frame
    void Update ()
    {
        //Update the camera reference if we're set to do that each frame. 
        if(CheckCameraReferenceEachFrame)
        {
            Camera newcamera = GetCamera.Invoke();
        }

        //Make sure the aspect ratio is what it should be - can change if you scale the plane's width, or mess with the camera at runtime. 
        if(TargetCamera.aspect != transform.localScale.x / transform.localScale.y)
        {
            MatchCameraAspectRatio();
        }

        RenderCameraToScreen(); //Draw the current camera frame to the rendertexture. 
	}

    /// <summary>
    /// Temporarily make the screen the camera's rendertexture and force a render. 
    /// This lets us capture the camera's image without forcing the camera to be dedicated to this one RenderTexture. 
    /// </summary>
    void RenderCameraToScreen()
    {
        if (!TargetCamera) return; //We've not nothing to render. 

        RenderTexture oldtexture = TargetCamera.targetTexture; //Cache the old setting for restoring later. (This is probably null)

        TargetCamera.targetTexture = _renderTexture;
        //RenderTexture.active = _renderTexture; //Read once that I needed to do this but I... guess not? ¯\_(ツ)_/¯
        TargetCamera.Render();

        TargetCamera.targetTexture = oldtexture; //Put the camera back how we found it. 
    }

    /// <summary>
    /// Scale's the plane's height so that the plane's dimensions match the camera aspect ratio. 
    /// </summary>
    void MatchCameraAspectRatio()
    {
        if (!TargetCamera) return; //No can do, chief. 

        float newyscale = transform.localScale.x / TargetCamera.aspect;
        transform.localScale = new Vector3(transform.localScale.x, newyscale, transform.localScale.z);
    }
}
