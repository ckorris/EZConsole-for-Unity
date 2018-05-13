using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Reflection;

[Serializable]
public class Console : MonoBehaviour
{
    public GameObject ScriptObject; //Can be assigned at compile- or runtime, but must contain a script assignable to the class of CompileTimeScript. 

    [SerializeField]
    public MonoScript CompileTimeScript; //For compile-time, and briefly at Start for reflection purposes. 
    private Component _runtimeScript; //The script attached to the gameobject from which we actually create the delegates. 


    public BaseDisplay TestDisplay; //Testing, remove later. 

    // Use this for initialization
    void Start()
    {
        _runtimeScript = ScriptObject.GetComponent(CompileTimeScript.GetClass()); //The non-generic form of GetComponent, mothafuckaz! 
        if (_runtimeScript == null)
        {
            //Put hard assert here later in HF
            Debug.LogError(name + ": Scriptobject " + ScriptObject.name + " doesn't contain script type " + CompileTimeScript.name + "!");
        }






        #region Hard-Coded Test
        if (false) //Change to turn this test on/off. 
        {
            //Hard-code adding a float function to make sure this works conceptually. 
            MethodInfo[] methods = CompileTimeScript.GetClass().GetMethods(BindingFlags.Instance | BindingFlags.Public); //Should be the last time we call CompileTimeScript at runtime. 

            //TEST: Find the base type of TestDisplay, cached for faster iteration when we loop through methods. 
            Type disptype = (Type)TestDisplay.GetType().GetMethod("get_DisplayType").Invoke(TestDisplay, null);

            //Skip any that aren't from the user-created component
            int validmethods = 0;
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].DeclaringType == typeof(MonoBehaviour) || methods[i].DeclaringType.IsAssignableFrom(typeof(MonoBehaviour)))
                {
                    continue;
                }
                validmethods++;
                //print(methods[i].Name + ", " + methods[i].DeclaringType);

                //Now for the actual hard-coded testing. 
                if (methods[i].ReturnType == disptype)
                {
                    print("Found match: " + disptype);

                    //Call RegisterIntoGenericDisplay with the found return type. 
                    //First cast it into a generic method so we can call it with a type known only at runtime - disptype. 
                    MethodInfo registermethod = typeof(Console).GetMethod("RegisterIntoGenericDisplay");
                    MethodInfo genericregistermethod = registermethod.MakeGenericMethod(disptype);

                    //Call the generic method and pass the method we want to turn into a delegate. 
                    genericregistermethod.Invoke(this, new object[] { methods[i], TestDisplay });
                }
            }
        }
        #endregion

    }

    // Update is called once per frame
    void Update()
    {

    }

    //More tests
    public void RegisterIntoGenericDisplay<T>(MethodInfo info, BaseDisplay display)
    {
        BaseDisplay<T> casteddisplay = display as BaseDisplay<T>;

        Func<T> casteddelegate = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), _runtimeScript, info);

        casteddisplay.RegisterDisplayDelegate(casteddelegate);
    }
}

[Serializable]
public struct DisplayBinding
{
    public BaseDisplay Display;
    public string MethodName;
}

