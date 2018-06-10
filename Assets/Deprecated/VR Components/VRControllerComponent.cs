using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class VRControllerComponent : MonoBehaviour
{
    public SteamVR_TrackedObject TrackedObject;

    public SteamVR_Controller.Device ThisController
    {
        get
        {
            return SteamVR_Controller.Input((int)TrackedObject.index);
        }
    }

    void Awake()
    {
        TrackedObject = GetComponentInParent<SteamVR_TrackedObject>();
    }


    #region Haptic Coroutines

    //For just one pulse
    public IEnumerator VibrateOnce(float strength, float duration)
    {
        for (float i = 0; i <= duration; i += Time.deltaTime)
        {
            ThisController.TriggerHapticPulse((ushort)Mathf.Lerp(0, 3999, strength));
            yield return null;
        }
    }

    //For when you want to repeat the same pulse several times, with the same time interval between them. 
    public IEnumerator VibrateMultiple(float strength, float pulseduration, int pulses, float gapduration)
    {
        for (int i = 0; i < pulses; i++)
        {
            if (i != 0) yield return new WaitForSeconds(gapduration);
            yield return StartCoroutine(VibrateOnce(strength, pulseduration));
        }
    }

    //Overload to let you use the Vibration class if you really want.
    public IEnumerator VibrateMultiple(Vibration vibration, int pulses)
    {
        for (int i = 0; i < pulses; i++)
        {
            if (i != 0) yield return new WaitForSeconds(vibration.gapUntilNext);
            yield return StartCoroutine(VibrateOnce(vibration.strength, vibration.duration));
        }
    }

    //For when you want a set of pulses, but each one is different and the time intervals between them are different. 
    //Something I (Chris) want to toy with a lot. You could give objects a haptic "signature" that could be especially
    //good for reaching for things you can't see because they're out of view or something. Should couple with sound, too. 
    public IEnumerator VibrateMultipleUneven(List<Vibration> vibrations)
    {
        foreach (Vibration vibe in vibrations)
        {
            yield return StartCoroutine(VibrateOnce(vibe.strength, vibe.duration));
            yield return new WaitForSeconds(vibe.gapUntilNext);
        }
    }

    //For when you want to play a vibration that matches a sound clip. 
    //Each frame, it'll play a pulse based on the strongest sample since the frame before it. 

    public IEnumerator VibrateToSound(AudioClip clip, bool playsound)
    {
        if (playsound)
        {
            AudioSource source = GetComponent<AudioSource>();
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
            }

            source.PlayOneShot(clip);
        }
        //if (ShipEditorSettings.Instance.AudioSyncedVibrations) //TODO: Fix this
        if (true)
        {
            int floatlength = clip.samples * clip.channels;
            float[] samples = new float[floatlength];
            clip.GetData(samples, 0);

            float samplespersecond = floatlength / clip.length;

            for (float i = 0; i < floatlength; i += Time.deltaTime * samplespersecond)
            {
                float highestsample = 0f;
                for (float j = i; j <= i + Time.deltaTime * samplespersecond; j++)
                {
                    if (j >= samples.Length) continue;
                    if (samples[Mathf.FloorToInt(j)] > highestsample) highestsample = samples[Mathf.FloorToInt(j)];
                }

                //print("Samples this frame: " + Time.deltaTime * samplespersecond);
                //print("Strength: " + highestsample);
                ThisController.TriggerHapticPulse((ushort)Mathf.Lerp(0, 3999, highestsample));

                yield return null;
            }
        }

    }

    #endregion
}

public struct Vibration
{
    public float strength;
    public float duration;
    public float gapUntilNext;
}
