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
    public Component RuntimeScript; //The script attached to the gameobject from which we actually create the delegates. 

    //Bindings lists
    [SerializeField]
    public List<DisplayBinding> DisplayBindingsList = new List<DisplayBinding>();
    [SerializeField]
    public List<ControlBinding> ControlBindingsList = new List<ControlBinding>();

    //Methodinfos of methods we'll turn generic. Cached for performance, because reflection. 
    MethodInfo _displayRegisterMethod;
    MethodInfo _controlRegisterMethod;

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
        Func<T> casteddelegate = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), RuntimeScript, info);
        casteddisplay.RegisterDisplayDelegate(RuntimeScript, casteddelegate);

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
        Action<T> casteddelegate = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), RuntimeScript, info);
        castedcontrol.RegisterControlDelegate(RuntimeScript, casteddelegate);

    }

    #endregion

}

[Serializable]
public struct DisplayBinding
{
    public string MethodName; //We store the name because a MethodInfo can't be serialized.
    public BaseDisplay Display;
}

[Serializable]
public struct ControlBinding
{
    public string MethodName; //We store the name because a MethodInfo can't be serialized.
    public BaseControl Control;
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
        DrawDefaultInspector();

        if(_script != _lastScript)
        {
            //The script got changed, clean house. 
            UpdateBindings();
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
        _console.RuntimeScript = (Component)EditorGUILayout.ObjectField(_console.RuntimeScript, _scriptType, true, null);
        EditorGUILayout.EndHorizontal();

        //Display bindings
        for(int i = 0; i < _console.DisplayBindingsList.Count; i++)
        {
            DisplayBinding dbind = _console.DisplayBindingsList[i]; //Shorthand
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(dbind.MethodName + ": ");

            //Figure out the BaseDisplay type 
            Type generic = typeof(BaseDisplay<>);
            Type constructed = generic.MakeGenericType(new Type[1] { _scriptType.GetMethod(dbind.MethodName).ReturnType }); //Too SLOW

            BaseDisplay basedisp = (BaseDisplay)EditorGUILayout.ObjectField(dbind.Display, constructed, true, null); //Not showing type

            EditorGUILayout.EndHorizontal();
            
            //If the bound object has changed, inject this binding in place of the old one. 
            if(dbind.Display != basedisp)
            {
                DisplayBinding replacebind = new DisplayBinding
                {
                    MethodName = dbind.MethodName,
                    Display = basedisp
                };
                _console.DisplayBindingsList[i] = replacebind;
                Debug.Log("Replaced");
            }
        }
    }

    private void UpdateBindings()
    {
        List<MethodInfo> dispmethods = new List<MethodInfo>();
        List<MethodInfo> contmethods = new List<MethodInfo>();

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
        //We'll make custom bindings for each - except if the current bindings list already has a binding that matches the name and type. 
        List<DisplayBinding> finaldispbinds = new List<DisplayBinding>();
        List<ControlBinding> finalcontbinds = new List<ControlBinding>();

        //Display first. 
        foreach(MethodInfo info in dispmethods)
        {
            bool foundoldmatch = false; 
            //Check existing bindings for both names and return types. 
            foreach(DisplayBinding oldbinding in _console.DisplayBindingsList)
            {
                if(oldbinding.MethodName == info.Name && _scriptType.GetMethod(oldbinding.MethodName).ReturnType == info.ReturnType)
                {
                    //It's a match. Pass in the old one. 
                    finaldispbinds.Add(oldbinding);
                    foundoldmatch = true;
                    break;
                }
            }

            if(foundoldmatch == false)
            {
                DisplayBinding newdisp = new DisplayBinding
                {
                    MethodName = info.Name
                };

                finaldispbinds.Add(newdisp);
            }
        }

        //Now Control. 
        foreach(MethodInfo info in contmethods)
        {
            bool foundoldmatch = false;
            ParameterInfo[] newparams = info.GetParameters(); // Caching because we'll compare a lot. 
            //Check existing bindings for both names and parameters.
            foreach(ControlBinding oldbinding in _console.ControlBindingsList)
            {
                if(oldbinding.MethodName == info.Name && _scriptType.GetMethod(oldbinding.MethodName).GetParameters() == newparams)
                {
                    //It's a match. Pass in the old one. 
                    finalcontbinds.Add(oldbinding);
                    foundoldmatch = true;
                    break;
                }
            }

            if(foundoldmatch == false)
            {

                ControlBinding newcont = new ControlBinding
                {
                    MethodName = info.Name
                };
                finalcontbinds.Add(newcont);
            }
        }

        //Replace the console's list with ours. 
        _console.DisplayBindingsList = finaldispbinds;
        _console.ControlBindingsList = finalcontbinds;

    }
}

