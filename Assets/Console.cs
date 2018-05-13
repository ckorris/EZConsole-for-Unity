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

    //Methodinfos of methods we'll turn generic. Cached for performance, because reflection. 
    MethodInfo _displayRegisterMethod;
    MethodInfo _controlRegisterMethod;

    public BaseDisplay TestDisplay; //Testing, remove later. 

    // Use this for initialization
    void Start()
    {
        _runtimeScript = ScriptObject.GetComponent(CompileTimeScript.GetClass()); //The non-generic form of GetComponent, mothafuckaz! 
        if (_runtimeScript == null)
        {
            //Good to put in a hard assert here
            Debug.LogError(name + ": Scriptobject " + ScriptObject.name + " doesn't contain script type " + CompileTimeScript.name + "!");
        }

        //Cache MethodInfos for the register methods, so we can make them generic later without repeating these particular reflection calls. 
        _displayRegisterMethod = typeof(Console).GetMethod("RegisterDisplayHelper", BindingFlags.Instance | BindingFlags.NonPublic);
        _controlRegisterMethod = typeof(Console).GetMethod("RegisterControlHelper", BindingFlags.Instance | BindingFlags.NonPublic);

        #region Hard-Coded Test
        if (true) //Change to turn this test on/off. 
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

                    RegisterIntoDisplay(methods[i], TestDisplay, disptype);
                    //Call RegisterIntoGenericDisplay with the found return type. 
                    //First cast it into a generic method so we can call it with a type known only at runtime - disptype. 
                    //MethodInfo genericregistermethod = _registerMethod.MakeGenericMethod(disptype);

                    //Call the generic method and pass the method we want to turn into a delegate. 
                    //genericregistermethod.Invoke(this, new object[] { methods[i], TestDisplay });
                }
            }
        }
        #endregion

    }

    // Update is called once per frame
    void Update()
    {

    }

    #region Display Register Methods
    /// <summary>
    /// Registers a method into a display by making RegisterDisplayHandler generic and passing the given type into it. 
    /// Calling this allows you to assign displays without handling generics yourself. 
    /// </summary>
    /// <param name="method"></param>
    /// <param name="disptype"></param>
    /// <param name="display"></param>
    private void RegisterIntoDisplay(MethodInfo method, BaseDisplay display, Type disptype)
    {
        MethodInfo registerhelpergeneric = _displayRegisterMethod.MakeGenericMethod(disptype);
        registerhelpergeneric.Invoke(this, new object[] { method, display });
    }

    /// <summary>
    /// Registers a method into a display by making RegisterDisplayHandler generic and passing the given type into it. 
    /// Calling this allows you to assign displays without handling generics yourself. 
    /// This overload uses reflection to get the display's type, so it's better to pass in the type if it's already known. 
    /// </summary>
    /// <param name="method"></param>
    /// <param name="display"></param>
    private void RegisterIntoDisplay(MethodInfo method, BaseDisplay display)
    {
        Type disptype = (Type)display.GetType().GetMethod("get_DisplayType").Invoke(display, null);
        RegisterIntoDisplay(method, display, disptype);
    }
    

    /// <summary>
    /// Called exclusively by RegisterIntoDisplay(). Casts the target display properly using T, then handles registration. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="info"></param>
    /// <param name="display"></param>
    private void RegisterDisplayHelper<T>(MethodInfo info, BaseDisplay display)
    {
        BaseDisplay<T> casteddisplay = display as BaseDisplay<T>; //Cast so we can interact with it directly - BaseDisplay is an empty class used for simple references. 
        Func<T> casteddelegate = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), _runtimeScript, info);
        casteddisplay.RegisterDisplayDelegate(_runtimeScript, casteddelegate);

    }
    #endregion

    #region Control Register Methods

    /// <summary>
    /// egisters a method into a control by making RegisterController generic and passing the given type into it. 
    /// Calling this allows you to assign controls without handling generics yourself.
    /// </summary>
    /// <param name="method"></param>
    /// <param name="control"></param>
    /// <param name="conttype"></param>
    private void RegisterIntoControl(MethodInfo method, BaseControl control, Type conttype)
    {
        MethodInfo registerhelpergeneric = _controlRegisterMethod.MakeGenericMethod(conttype);
        registerhelpergeneric.Invoke(this, new object[] { method, control });
    }

    /// <summary>
    /// Registers a method into a control by making RegisterControlHandler generic and passing the given type into it. 
    /// Calling this allows you to assign controls without handling generics yourself. 
    /// This overload uses reflection to get the control's type, so it's better to pass in the type if it's already known. 
    /// </summary>
    /// <param name="method"></param>
    /// <param name="control"></param>
    private void RegisterIntoControl(MethodInfo method, BaseControl control)
    {
        Type conttype = (Type)control.GetType().GetMethod("get_ControlType").Invoke(control, null);
        RegisterIntoControl(method, control, conttype);
    }

    /// <summary>
    /// Called exclusively by RegisterIntoControl(). Casts the target control properly using T, then handles registration. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="info"></param>
    /// <param name="control"></param>
    private void RegisterControlHelper<T>(MethodInfo info, BaseControl control)
    {
        BaseControl<T> castedcontrol = control as BaseControl<T>; //Cast so we can interact with it directly - BaseControl is an empty class used for simple references. 
        Action<T> casteddelegate = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), _runtimeScript, info);
        castedcontrol.RegisterControlDelegate(_runtimeScript, casteddelegate);

    }

    #endregion

}

[Serializable]
public struct DisplayBinding
{
    public BaseDisplay Display;
    public string MethodName;
}

