using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Reflection;

[Serializable]
public class Console : MonoBehaviour
{
    //public GameObject ScriptObject; //Can be assigned at compile- or runtime, but must contain a script assignable to the class of CompileTimeScript. 

    [SerializeField]
    public MonoScript CompileTimeScript; //For compile-time, and briefly at Start for reflection purposes. 
    [SerializeField]
    private Component _runtimeScript; //The script attached to the gameobject from which we actually create the delegates. 
    public Component RuntimeScript
    {
        get
        {
            return _runtimeScript;
        }
    }

    //Bindings lists
    [SerializeField]
    public List<DisplayBinding> DisplayBindingsList = new List<DisplayBinding>();
    [SerializeField]
    public List<ControlBinding> ControlBindingsList = new List<ControlBinding>();

    //Methodinfos of methods we'll turn generic. Cached for performance, because reflection. 
    MethodInfo _displayRegisterMethod;
    MethodInfo _controlRegisterMethod;
    MethodInfo _displayDeregisterMethod;
    MethodInfo _controlDeregisterMethod;

    //Testing, remove later. 
    public BaseDisplay TestDisplay;
    public BaseControl TestControl;

    // Use this for initialization
    void Start()
    {
        /*RuntimeScript = ScriptObject.GetComponent(CompileTimeScript.GetClass()); //The non-generic form of GetComponent, mothafuckaz! 
        if (RuntimeScript == null)
        {
            //Good to put in a hard assert here
            Debug.LogError(name + ": Scriptobject " + ScriptObject.name + " doesn't contain script type " + CompileTimeScript.name + "!");
        }*/

        //Cache MethodInfos for the register methods, so we can make them generic later without repeating these particular reflection calls. 
        _displayRegisterMethod = typeof(Console).GetMethod("RegisterDisplayHelper", BindingFlags.Instance | BindingFlags.NonPublic);
        _controlRegisterMethod = typeof(Console).GetMethod("RegisterControlHelper", BindingFlags.Instance | BindingFlags.NonPublic);
        _displayDeregisterMethod = typeof(Console).GetMethod("DeregisterDisplayHelper", BindingFlags.Instance | BindingFlags.NonPublic);
        _controlDeregisterMethod = typeof(Console).GetMethod("DeregisterControlHelper", BindingFlags.Instance | BindingFlags.NonPublic);

        #region Hard-Coded Test 
        if (false) //Change to turn this test on/off. 
        {
            //Hard-code adding a float function to make sure this works conceptually. 
            MethodInfo[] methods = CompileTimeScript.GetClass().GetMethods(BindingFlags.Instance | BindingFlags.Public); //Should be the last time we call CompileTimeScript at runtime. 

            //TEST: Find the base type of TestDisplay, cached for faster iteration when we loop through methods. 
            Type disptype = (Type)TestDisplay.GetType().GetMethod("get_DisplayType").Invoke(TestDisplay, null);
            Type conttype = (Type)TestControl.GetType().GetMethod("get_ControlType").Invoke(TestControl, null);



            //Skip any that aren't from the user-created component
            int validmethods = 0;
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].DeclaringType == typeof(MonoBehaviour) || methods[i].DeclaringType.IsAssignableFrom(typeof(MonoBehaviour)))
                {
                    continue;
                }
                validmethods++;

                //Now for the actual hard-coded testing. 
                //Display:
                if (methods[i].ReturnType == disptype)
                {
                    RegisterIntoDisplay(methods[i], TestDisplay, disptype);
                }
                //Control:
                ParameterInfo[] paramarray = methods[i].GetParameters();
                if (paramarray.Length == 1 && paramarray[0].ParameterType == conttype)
                {
                    print("Found control match: " + conttype + " in " + methods[i].Name);
                    RegisterIntoControl(methods[i], TestControl, conttype);
                }
            }
        }
        #endregion

        //Actually set up the bindings
        SetUpBindings();

    }

    public void ChangeRuntimeScript(Component newscript) //Method instead of a setter as it involves lots of reflection. 
    {
        if (newscript != _runtimeScript)
        {
            if (Application.isPlaying)
            {
                if (_runtimeScript != null)
                {
                    DeregisterAll(_runtimeScript);
                }

                _runtimeScript = newscript;

                if (_runtimeScript != null)
                {
                    SetUpBindings();
                }
            }
            else
            {
                _runtimeScript = newscript;
            }
        }
    }

    private void SetUpBindings()
    {
        //Display bindings first
        foreach (DisplayBinding dispbind in DisplayBindingsList)
        {
            if (dispbind.Display)
            {
                MethodInfo method = CompileTimeScript.GetClass().GetMethod(dispbind.MethodName);
                RegisterIntoDisplay(method, dispbind.Display);
            }
        }

        //Control bindings
        foreach (ControlBinding contbind in ControlBindingsList)
        {
            if (contbind.Control)
            {
                MethodInfo method = CompileTimeScript.GetClass().GetMethod(contbind.MethodName);
                RegisterIntoControl(method, contbind.Control);
            }
        }
    }

    public void DeregisterAll(Component oldcomponent)
    {
        //Display
        foreach (DisplayBinding dispbind in DisplayBindingsList)
        {
            if (dispbind.Display == null) //No display to deregister
            {
                continue;
            }

            Type disptype = (Type)dispbind.Display.GetType().GetMethod("get_DisplayType").Invoke(dispbind.Display, null);
            MethodInfo deregisterhelpergeneric = _displayDeregisterMethod.MakeGenericMethod(disptype);
            deregisterhelpergeneric.Invoke(this, new object[] { oldcomponent, dispbind.Display });
        }

        //Control
        foreach (ControlBinding contbind in ControlBindingsList)
        {
            if (contbind.Control == null) //No control to deregister
            {
                continue;
            }

            Type conttype = (Type)contbind.Control.GetType().GetMethod("get_ControlType").Invoke(contbind.Control, null);
            MethodInfo deregisterhelpergeneric = _controlDeregisterMethod.MakeGenericMethod(conttype);
            deregisterhelpergeneric.Invoke(this, new object[] { oldcomponent, contbind.Control });
        }
    }


    #region Display Register/Deregister Methods
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

    /// <summary>
    /// Deregisters a component from a selected display. 
    /// Called by Deregister functions, where it gets cast as a generic method, then T can be set properly. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="component"></param>
    /// <param name="display"></param>
    private void DeregisterDisplayHelper<T>(Component component, BaseDisplay display)
    {
        BaseDisplay<T> casteddisplay = display as BaseDisplay<T>; //Cast so we can interact with it directly - BaseDisplay is an empty class used for simple references. 
        casteddisplay.Deregister(display);
    }
    #endregion

    #region Control Register/Deregister Methods

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

    /// <summary>
    /// Deregsiters a component from a selected control. 
    /// Called by Deregister functions, where it gets cast as a generic method, then T can be set properly. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="component"></param>
    /// <param name="control"></param>
    private void DeregisterControlHelper<T>(Component component, BaseControl control)
    {
        BaseControl<T> castedcontrol = control as BaseControl<T>;
        castedcontrol.Deregister(component);
    }
    #endregion

}

[Serializable]
public struct DisplayBinding
{
    public string MethodName; //We store the name because a MethodInfo can't be serialized.
    public BaseDisplay Display;
    public string TypeString; //The type of the method displayed. As a string so we can efficiently display it in the editor. 
}

[Serializable]
public struct ControlBinding
{
    public string MethodName; //We store the name because a MethodInfo can't be serialized.
    public BaseControl Control;
    public string ParamString; //The types of the method's parameters. As a string so we can efficiently display it in the editor. 
}


[CustomEditor(typeof(Console))]
public class ConsoleEditor : Editor
{
    Console _console; //The console we're editing

    //Shorthand
    MonoScript _script
    {
        get
        {
            if (_console.CompileTimeScript)
            {
                return _console.CompileTimeScript;
            }
            else return null;
        }
        set
        {
            _console.CompileTimeScript = value;
        }
    }
    Type _scriptType
    {
        get
        {
            if (_script)
            {
                return _script.GetClass();
            }
            else return null;
        }
    }

    MonoScript _lastScript; //Used to check if the current script has changed

    private void OnEnable()
    {
        _console = (Console)target;
        _lastScript = _script;

        UpdateBindings();
    }

    public override void OnInspectorGUI()
    {
        //DrawDefaultInspector();

        if (_script != _lastScript)
        {
            //The script got changed, clean house. 
            UpdateBindings();
            _lastScript = _script;
        }

        //Don't allow changing the target script during runtime
        EditorGUI.BeginDisabledGroup(Application.isPlaying);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Target Monoscript: ", EditorStyles.boldLabel);
        _script = (MonoScript)EditorGUILayout.ObjectField(_script, typeof(MonoScript), false, null);
        EditorGUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();

        //Target component
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Runtime Component: ", EditorStyles.boldLabel);
        //_console.RuntimeScript = (Component)EditorGUILayout.ObjectField(_console.RuntimeScript, _scriptType, true, null);
        Component newcomponent = (Component)EditorGUILayout.ObjectField(_console.RuntimeScript, _scriptType, true, null);
        if (newcomponent != _console.RuntimeScript)
        {
            _console.ChangeRuntimeScript(newcomponent);
        }

        EditorGUILayout.EndHorizontal();

        //Display bindings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Display Properties", EditorStyles.boldLabel);

        for (int i = 0; i < _console.DisplayBindingsList.Count; i++)
        {
            DisplayBinding dbind = _console.DisplayBindingsList[i]; //Shorthand

            EditorGUILayout.BeginHorizontal();

            //Figure out the BaseDisplay type 
            Type generic = typeof(BaseDisplay<>);
            Type constructed = generic.MakeGenericType(new Type[1] { _scriptType.GetMethod(dbind.MethodName).ReturnType }); //This gets called most every frame, so would be nice to optimize later. 

            EditorGUILayout.LabelField(dbind.MethodName + " <" + dbind.TypeString + ">"); //Adding type in label since the object field won't show it properly due to serialization

            BaseDisplay basedisp = (BaseDisplay)EditorGUILayout.ObjectField(dbind.Display, constructed, true, null);

            EditorGUILayout.EndHorizontal();

            //If the bound object has changed, inject this binding in place of the old one. 
            if (dbind.Display != basedisp)
            {
                DisplayBinding replacebind = new DisplayBinding
                {
                    MethodName = dbind.MethodName,
                    Display = basedisp,
                    TypeString = dbind.TypeString
                };
                _console.DisplayBindingsList[i] = replacebind;
                Debug.Log("Replaced");
            }
        }

        //Control bindings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Control Properties/Methods", EditorStyles.boldLabel);

        for (int i = 0; i < _console.ControlBindingsList.Count; i++)
        {
            ControlBinding cbind = _console.ControlBindingsList[i]; //Shorthand
            EditorGUILayout.BeginHorizontal();

            //Figure out the BaseControl type
            Type generic = typeof(BaseControl<>);
            ParameterInfo[] paramsinfo = _scriptType.GetMethod(cbind.MethodName).GetParameters();
            Type newtype = (paramsinfo.Length > 0) ? paramsinfo[0].ParameterType : typeof(void);

            Type constructed = generic.MakeGenericType(new Type[1] { newtype }); //This gets called most every frame, so would be nice to optimize later. //WRONG fix later

            EditorGUILayout.LabelField(cbind.MethodName + " <" + cbind.ParamString + ">"); //Adding type in label since the object field won't show it properly due to serialization

            BaseControl basecont = (BaseControl)EditorGUILayout.ObjectField(cbind.Control, constructed, true, null);

            EditorGUILayout.EndHorizontal();

            //If the bound object has changed inject this binding in place of old one. 
            if (cbind.Control != basecont)
            {
                ControlBinding replacebind = new ControlBinding
                {
                    MethodName = cbind.MethodName,
                    Control = basecont,
                    ParamString = cbind.ParamString
                };
                _console.ControlBindingsList[i] = replacebind;
                Debug.Log("Replaced");
            }
        }
    }

    private void UpdateBindings()
    {
        List<MethodInfo> dispmethods = new List<MethodInfo>();
        List<MethodInfo> contmethods = new List<MethodInfo>();

        //We'll make custom bindings for each - except if the current bindings list already has a binding that matches the name and type. 
        List<DisplayBinding> finaldispbinds = new List<DisplayBinding>();
        List<ControlBinding> finalcontbinds = new List<ControlBinding>();

        //Stop if we don't have a script
        if (_script != null)
        {
            //Iterate through methods and assign relevant ones to bindings list
            MethodInfo[] methods = _scriptType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            for (int i = 0; i < methods.Length; i++)
            {
                //Don't do anything if it's part of MonoBehaviour, because the developer didn't add that. 
                if (methods[i].DeclaringType.IsAssignableFrom(typeof(MonoBehaviour)))
                {
                    continue;
                }

                //Assign everything into a relevant list, if there is one. 
                if (methods[i].ReturnType == typeof(void))
                {
                    //No return type. It can fit a control. 
                    contmethods.Add(methods[i]);
                }
                else if (methods[i].GetParameters().Length == 0)
                {
                    //It has a return, but takes no parameters. It's either a getter or a method made to act like one. 
                    dispmethods.Add(methods[i]);
                }
            }

            //Now we've got all the methods that need bindings. 


            //Display first. 
            foreach (MethodInfo info in dispmethods)
            {
                bool foundoldmatch = false;
                //Check existing bindings for both names and return types. 
                foreach (DisplayBinding oldbinding in _console.DisplayBindingsList)
                {
                    if (oldbinding.MethodName == info.Name && _scriptType.GetMethod(oldbinding.MethodName).ReturnType == info.ReturnType)
                    {
                        //It's a match. Pass in the old one. 
                        finaldispbinds.Add(oldbinding);
                        foundoldmatch = true;
                        break;
                    }
                }

                if (foundoldmatch == false)
                {
                    DisplayBinding newdisp = new DisplayBinding
                    {
                        MethodName = info.Name,
                        TypeString = info.ReturnType.Name
                    };

                    finaldispbinds.Add(newdisp);
                }
            }

            //Now Control. 
            foreach (MethodInfo info in contmethods)
            {
                bool foundoldmatch = false;
                ParameterInfo[] newparams = info.GetParameters(); // Caching because we'll compare a lot. 
                                                                  //Check existing bindings for both names and parameters.
                foreach (ControlBinding oldbinding in _console.ControlBindingsList)
                {
                    //if (oldbinding.MethodName == info.Name && _scriptType.GetMethod(oldbinding.MethodName).GetParameters() == newparams)
                    if (oldbinding.MethodName == info.Name)
                    {
                        //The names match. Make sure the parameter types match. 
                        ParameterInfo[] oldparams = _scriptType.GetMethod(oldbinding.MethodName).GetParameters();

                        if (newparams.Length != oldparams.Length)
                        {
                            continue;
                        }

                        bool foundbadparam = false; 
                        for (int i = 0; i < newparams.Length; i++)
                        {
                            if (newparams[i].ParameterType != oldparams[i].ParameterType)
                            {
                                foundbadparam = true;
                            }
                        }
                        if(foundbadparam)
                        {
                            continue;
                        }


                        //It's a match. Pass in the old one. 
                        finalcontbinds.Add(oldbinding);
                        foundoldmatch = true;
                        break;
                    }
                }

                if (foundoldmatch == false)
                {
                    //Make a string that lists the parameters for easy display
                    string paramstring = "";
                    for (int i = 0; i < newparams.Length; i++)
                    {
                        paramstring += newparams[i].ParameterType.Name;
                        if (i < newparams.Length - 1)
                        {
                            paramstring += ", ";
                        }
                    }
                    ControlBinding newcont = new ControlBinding
                    {
                        MethodName = info.Name,
                        ParamString = paramstring
                    };

                    finalcontbinds.Add(newcont);
                }
            }

        }
        //Replace the console's list with ours. 
        _console.DisplayBindingsList = finaldispbinds;
        _console.ControlBindingsList = finalcontbinds;

    }
}

