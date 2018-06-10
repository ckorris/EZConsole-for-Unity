using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;


[Serializable]
public class Console : MonoBehaviour
{
    //TODO: Rename to ScriptType and ScriptInstance along with relevant methods like ChangeRuntimeScript(). 
    [SerializeField]
    public MonoScript CompileTimeScript; //For compile-time, and briefly at Start for reflection purposes. 
    [SerializeField]
    public Component RuntimeScript; //The script attached to the gameobject from which we actually create the delegates. 

    [SerializeField]
    public List<EZConsoleBindingSerial> Bindings;

    MethodInfo _registerMethod; //TMethodInfo for making a generic version of RegisterDelegateHelper<T>. Caching saves on reflection. 

    // Use this for initialization
    void Start()
    {
        //Cache the method that registers a delegate. (May need to turn into list if we use the 32 brute force Func/Action thingy.)
        _registerMethod = typeof(Console).GetMethod("RegisterDelegateHelper", BindingFlags.Instance | BindingFlags.NonPublic);

        SetUpBindings(); //Turn bindings into actual registrations. 
    }

    private void SetUpBindings()
    {
        //TODO: Store methodinfos in a dictionary with the name as a key so multiple delegates on one method only need one reflection call. 
        foreach (EZConsoleBindingSerial binding in Bindings)
        {
            MethodInfo minfo = RuntimeScript.GetType().GetMethod(binding.TargetMethodName); //The method to register
            FieldInfo finfo = binding.ControlComponent.GetType().GetField(binding.ControlDelegateName); //The delegate to add the method to

            //TODO: Try replicating old register methods, but pass the whole delegate type as T instead of the delegate argument type. 
            RegisterDelegate(minfo, finfo, binding.ControlComponent);
        }
    }

    /// <summary>
    /// Change runtime script during runtime, for use outside the Inspector. 
    /// Overload assumes RuntimeScript is still the "oldscript" which is not the case when the Inspector needs to call it because stupidity (read: serialization). 
    /// </summary>
    /// <param name="newscript"></param>
    public void ChangeRuntimeScript(Component newscript)
    {
        ChangeRuntimeScript(RuntimeScript, newscript);
    }

    /// <summary>
    /// Handle changing scripts from the old to the new. Designed for the editor to use. (See overload for runtime use)
    /// This is because the editor itself will have already changed RuntimeScript (necessarily for serialization) so we pass in the reference. 
    /// </summary>
    /// <param name="oldscript"></param>
    /// <param name="newscript"></param>
    public void ChangeRuntimeScript(Component oldscript, Component newscript)
    {
        if (newscript != RuntimeScript)
        {
            if (Application.isPlaying) //Didn't change via editor
            {
                if (RuntimeScript != null) //Remove old bindings
                {
                    //TODO: Deregister existing bindings
                    Debug.LogWarning("Note: Unsubscribing old bindings not yet implemented. Bindings from last script are still registered.");
                }

                RuntimeScript = newscript;

                if (RuntimeScript != null) //Check if we have an object to set up bindings to
                {
                    SetUpBindings();
                }
            }
            else
            {
                RuntimeScript = newscript;
            }
        }
    }

    /// <summary>
    /// Subscribes a method (minfo) into a delegate (finfo) on target component. 
    /// </summary>
    /// <param name="minfo"></param>
    /// <param name="finfo"></param>
    /// <param name="component"></param>
    private void RegisterDelegate(MethodInfo minfo, FieldInfo finfo, Component component)
    {
        //Find the return type of the target method
        Type returntype = minfo.ReturnType;

        //Make an array of types that include parameter types, with one at the end that represents the return type. 
        //This will get used to find the delegate type we'll need, and follows the format required by Func. 
        ParameterInfo[] paraminfos = minfo.GetParameters();
        Type[] paramtypes = new Type[paraminfos.Length];
        for (int p = 0; p < paraminfos.Length; p++)
        {
            paramtypes[p] = paraminfos[p].ParameterType;
        }

        //Get the type of delegate that we'll need to assign to this. 
        Type deltype;
        if (returntype == typeof(void)) //Action type
        {
            deltype = Expression.GetActionType(paramtypes);
        }
        else //Func type
        {
            Array.Resize(ref paramtypes, paramtypes.Length + 1);
            paramtypes[paramtypes.Length - 1] = returntype;

            deltype = Expression.GetFuncType(paramtypes);
        }

        //Make a generic method for the exact type we need
        MethodInfo registermethodgeneric = _registerMethod.MakeGenericMethod(deltype);
        registermethodgeneric.Invoke(this, new object[] { minfo, finfo, component });
    }

    /// <summary>
    /// The final step of RegisterDelegate(), which needs to be generic. 
    /// Don't call directly but use _registerMethod.MakeGenericMethod(type) instead, where type = the Func/Action type. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="metinfo"></param>
    /// <param name="finfo"></param>
    /// <param name="component"></param>
    private void RegisterDelegateHelper<T>(MethodInfo metinfo, FieldInfo finfo, Component component)
    {
        //To test. May need to make 16 versions for Action<T1,T2,T3> etc. and same with Func. But I want to avoid that. 
        Delegate d = Delegate.CreateDelegate(typeof(T), RuntimeScript, metinfo.Name);

        //Register into field
        MulticastDelegate targetdelegate = finfo.GetValue(component) as MulticastDelegate; //Get the target list
        Delegate combodelegate = Delegate.Combine(d, targetdelegate); //Add the new delegate to the invocation list of the old one
        finfo.SetValue(component, combodelegate); //Set the delegate to the combined list
    }
}

/// <summary>
/// Custom editor for the console. 
/// Most complexity arises from need to assign to serialized properties. 
/// </summary>
[CustomEditor(typeof(Console))]
public class ConsoleV3Editor : Editor
{
    Console _console; //The console we're editing, for speedy access. Don't write values via this if they need to actually be saved. 

    SerializedObject _consoleSerial; //The serialized object version for applying certain properties that don't like to serialize.
    SerializedProperty _compileScriptSerial; //Console.CompiletimeScript
    SerializedProperty _runtimeScriptSerial; //Console.RuntimeScript
    SerializedProperty _bindingsSerial; //Console.Bindings

    //Runtime script shorthand (non-serialized)
    MonoScript _script
    {
        get
        {
            if (_console != null && _console.CompileTimeScript != null)
            {
                return _console.CompileTimeScript;
            }
            else return null;
        }
    }

    //Script type shorthand
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
        //Non-serialized versions for faster access when possible. 
        _console = (Console)target;
        _lastScript = _script;

        //Serialized properties. If you want something you change to stay changed, modify these. 
        _consoleSerial = new SerializedObject(target);
        _compileScriptSerial = _consoleSerial.FindProperty("CompileTimeScript");
        _runtimeScriptSerial = _consoleSerial.FindProperty("RuntimeScript");
        _bindingsSerial = _consoleSerial.FindProperty("Bindings");
    }

    public override void OnInspectorGUI()
    {
        GUILayoutOption[] layoutoptions = new GUILayoutOption[2]
        {
            GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth / 2),
            GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight)
        };

        #region Target Script Labels/Fields
        //Target monoscript (compile-time script) object field
        //Don't allow changing the target monoscript during runtime
        EditorGUI.BeginDisabledGroup(Application.isPlaying);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Target Monoscript: ", EditorStyles.boldLabel, layoutoptions);
        EditorGUILayout.ObjectField(_compileScriptSerial, typeof(MonoScript), GUIContent.none, layoutoptions);
        _consoleSerial.ApplyModifiedProperties();
        EditorGUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();

        //Target component (runtime script) object field
        if (_script != null)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Runtime Component: ", EditorStyles.boldLabel, layoutoptions);
            Component oldcomponent = _console.RuntimeScript; //Cache this so we can run ChangeRuntimeScript if needed
            EditorGUILayout.ObjectField(_runtimeScriptSerial, _scriptType, GUIContent.none, layoutoptions);
            _consoleSerial.ApplyModifiedProperties();
            if (Application.isPlaying && oldcomponent != _console.RuntimeScript)
            {
                _console.ChangeRuntimeScript(oldcomponent, _console.RuntimeScript);
            }
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        EditorGUILayout.Separator();

        //Create a list for each target 
        if (_script != null)
        {
            //Make the dictionary of editorbindings, which represents the lists only in the editor.
            Dictionary<MethodInfo, List<EZConsoleBindingEditor>> editorbindings = new Dictionary<MethodInfo, List<EZConsoleBindingEditor>>();

            #region Populate Editor Bindings Dictionary
            //Get list of methods using reflection. 
            //Yes, it's calling this reflection every frame, but it's ONE inspector window at a time so you'd need like 100k+ methods to notice it. 
            //Alternatives would offer little performance gain in exchange for lots of script complexity. 
            MethodInfo[] methods = _scriptType.GetMethods(BindingFlags.Instance | BindingFlags.Public) 
                .Where(x => !x.DeclaringType.IsAssignableFrom(typeof(MonoBehaviour)))
                .ToArray();

            for (int i = 0; i < methods.Length; i++)
            {
                editorbindings.Add(methods[i], new List<EZConsoleBindingEditor>());
            }

            //Iterate through the serialized bindings list and make an EZConsoleBindingEditor item for each one.
            for (int i = 0; i < _bindingsSerial.arraySize; i++)
            {
                SerializedProperty sprop = _bindingsSerial.GetArrayElementAtIndex(i);
                string methodname = sprop.FindPropertyRelative("TargetMethodName").stringValue;
                MethodInfo minfokey = editorbindings.Keys.FirstOrDefault(m => m.Name == methodname);

                if (minfokey != null) //Make sure we have a list available.
                {
                    //We do. Make a new editor binding and add it. 
                    EZConsoleBindingEditor ebind = new EZConsoleBindingEditor()
                    {
                        ControlComponent = (Component)sprop.FindPropertyRelative("ControlComponent").objectReferenceValue,
                        ControlObject = (GameObject)sprop.FindPropertyRelative("ControlObject").objectReferenceValue,
                        ControlDelegateName = sprop.FindPropertyRelative("ControlDelegateName").stringValue,
                        SerialBinding = sprop
                    };

                    editorbindings[minfokey].Add(ebind);
                }
                else
                {
                    Debug.Log("Couldn't find a method in the list called " + methodname);
                }
            }
            #endregion

            #region Draw ReorderableLists
            //Now we've got a dictionary of editor bindings, and each binding points to a serialized property. Time to draw the lists. 
            foreach (MethodInfo minfo in editorbindings.Keys)
            {
                //Find the parameters and return type of the target method
                Type returntype = minfo.ReturnType;
                ParameterInfo[] paraminfos = minfo.GetParameters();

                //Make an array of types that include parameter types, with one at the end that represents the return type. 
                //This will get used to find the delegate type we'll need, and follows the format required by Func. 
                Type[] paramtypes = new Type[paraminfos.Length];
                for (int p = 0; p < paraminfos.Length; p++)
                {
                    paramtypes[p] = paraminfos[p].ParameterType;
                }

                //Get the type of delegate that we'll need to assign to this. 
                Type deltype;
                if (returntype == typeof(void)) //Action type
                {
                    deltype = Expression.GetActionType(paramtypes);
                }
                else //Func type
                {
                    Array.Resize(ref paramtypes, paramtypes.Length + 1);
                    paramtypes[paramtypes.Length - 1] = returntype;

                    deltype = Expression.GetFuncType(paramtypes);
                }

                //Make the list 
                ReorderableList reorderablelist = new ReorderableList(editorbindings[minfo], typeof(EZConsoleBindingEditor), true, true, true, true);

                #region ReorderableList Callback Assignments
                //Now we assign to all the ReorderableList's callbacks that we need. Unity handles when they're fired. 
                //Add label to the header
                reorderablelist.drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width / 2, rect.height), minfo.Name, EditorStyles.boldLabel);

                    //Make a string to represent the delegate type. (If we just convert to string, it'll be "Func`1" or some garbage
                    string deltypename;
                    if (deltype.Name.Contains("Action")) deltypename = "Action<";
                    else deltypename = "Func<";
                    foreach(ParameterInfo pinfo in paraminfos)
                    {
                        deltypename += ConvertToSimpleName(pinfo.ParameterType) + ", ";
                    }
                    
                    if (minfo.ReturnType != typeof(void) && minfo.ReturnType != null)
                    {
                        
                        deltypename += ConvertToSimpleName(minfo.ReturnType);
                    }
                    else
                    {

                        int? last = deltypename.LastIndexOf(", ");
                        if ((int)last > 0)
                        {
                            deltypename = deltypename.Remove((int)last, 2); //Remove the last comma we added //(new char[2] { ',', ' ' }); 
                        }
                        
                    }

                    deltypename += ">";
                    GUIStyle italicfontstyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Italic };
                    EditorGUI.LabelField(new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, rect.height), deltypename, italicfontstyle);
                    
                    EditorGUILayout.EndHorizontal();
                };

                //We can always press the remove button for now because it's glitchy as hell in Unity. 
                reorderablelist.onCanRemoveCallback = (list) => { return list.list.Count > 0; };

                //Pressing the add button should add to the serialized list, not the editor binding list. 
                reorderablelist.onAddCallback = (list) =>
                {
                    _bindingsSerial.arraySize += 1;
                    _bindingsSerial.GetArrayElementAtIndex(_bindingsSerial.arraySize - 1).FindPropertyRelative("TargetMethodName").stringValue = minfo.Name;
                };

                //Pressing the delete button, likewise, removes from the serialized list. But we have to properly point to the last item with this methodinfo.
                reorderablelist.onRemoveCallback = (list) =>
                {
                    if (list.count <= 0) return;

                    EZConsoleBindingEditor lastebind = (EZConsoleBindingEditor)list.list[list.list.Count - 1];
                    //Find the index of lastebind's serializedproperty in _bindingsSerial

                    bool foundit = false; //For error reporting
                    for(int i = 0; i < _bindingsSerial.arraySize; i++)
                    {
                        if(_bindingsSerial.GetArrayElementAtIndex(i).FindPropertyRelative("ControlDelegateName").stringValue == lastebind.ControlDelegateName)
                        {
                            _bindingsSerial.DeleteArrayElementAtIndex(i); //This works because it doesn't get applied until next frame. 
                            foundit = true;
                            break;
                        }
                    }
                    if(foundit == false) //Report error if we couldn't find it. 
                    {
                        Debug.LogError("Tried to delete " + lastebind.ControlDelegateName + " binding but couldn't find it in the serialized list.");
                    }
                };

                //Drawing is gonna be complicated. We're drawing based on the editorbinding, but we have to make sure changes write to the serializedproperty. 
                reorderablelist.drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    //Setup editor binding and serialized properties
                    EZConsoleBindingEditor ebind = editorbindings[minfo][index];
                    SerializedProperty controlobjectserial = ebind.SerialBinding.FindPropertyRelative("ControlObject");
                    SerializedProperty controlcomponentserial = ebind.SerialBinding.FindPropertyRelative("ControlComponent");
                    SerializedProperty delegatenameserial = ebind.SerialBinding.FindPropertyRelative("TargetDelegateName");

                    //Control object
                    Rect controlobjectrect = new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight);
                    ebind.ControlObject = (GameObject)EditorGUI.ObjectField(controlobjectrect, ebind.ControlObject, typeof(UnityEngine.Object), true);
                    controlobjectserial.objectReferenceValue = ebind.ControlObject;

                    #region Component/Delegate Drop-Down 
                    //Drop-down button for choosing the specific component/delegate. Similar to assigning events to Unity's Button UI. 
                    EditorGUI.BeginDisabledGroup(ebind.ControlObject == null);
                    //Set the name you see on the non-expanded version be either the target delegate name or "No Function." 
                    string dropdowntext;
                    if (ebind.ControlDelegateName == "" || ebind.ControlDelegateName == null)
                    {
                        dropdowntext = "No Function";
                    }
                    else
                    {
                        dropdowntext = ebind.ControlDelegateName;
                    }
                    GUIContent dropdowncontent = new GUIContent(dropdowntext);

                   
                    Rect dropdownrect = new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight);
                    if (EditorGUI.DropdownButton(dropdownrect, dropdowncontent, FocusType.Keyboard))
                    {
                        GenericMenu menu = new GenericMenu();

                        //Add the "No Component" option
                        MenuSelectComponent emptymsc = new MenuSelectComponent()
                        {
                            binding = ebind,
                            bindobject = ebind.ControlObject,
                            component = null,
                            delegatename = null
                        };
                        menu.AddItem(new GUIContent("None"), ebind.ControlComponent == null, SelectFunction, emptymsc);

                        menu.AddSeparator("");

                        //List all components
                        Component[] components = ebind.ControlObject.GetComponents<Component>();
                        for(int j = 0; j < components.Length; j++)
                        {
                            //List all delegates in the control, whihc includes actions, functions, etc. 
                            List<FieldInfo> fieldinfos = components[j].GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
                            .Where(t => t.FieldType.IsAssignableFrom(deltype))
                            .ToList();

                            foreach(FieldInfo finfo in fieldinfos)
                            {
                                MenuSelectComponent msc = new MenuSelectComponent()
                                {
                                    binding = ebind,
                                    component = components[j],
                                    bindobject = ebind.ControlObject,
                                    delegatename = finfo.Name
                                };

                                string path = components[j].GetType().Name + "/" + finfo.Name;
                                menu.AddItem(new GUIContent(path), ebind.ControlComponent == components[j], SelectFunction, msc);
                            }
                        }

                        menu.ShowAsContext();
                    }

                    EditorGUI.EndDisabledGroup();
                    #endregion

                };
                #endregion

                reorderablelist.index = reorderablelist.count - 1; //We need this for the removal button to work, because the index isn't set in any sane or observable way in Unity. 
                reorderablelist.DoLayoutList(); //Display the list.
            }
            #endregion

            _consoleSerial.ApplyModifiedProperties(); //Updates the SerializedObject that holds all the properties - they don't actually change until you do this. 
        }
    }

    /// <summary>
    /// When you select a component in the Bindings List menu, it calls this. Note that choosing "No Function" will pass the GameObject, 
    /// hence why it takes Object as an argument instead of component. 
    /// </summary>
    /// <param name="binding"></param>
    /// <param name="component"></param>
    public void SelectFunction(object menuselectcomponent)
    {
        if (menuselectcomponent.GetType() != typeof(MenuSelectComponent))
        {
            Debug.Log("Tried to pass something that wasn't a MenuSelectComponent into SelectComponent");
            return;
        }

        MenuSelectComponent msc = (MenuSelectComponent)menuselectcomponent;

        //Update the ebinding (probably unnecessary - try cleaning later)
        msc.binding.ControlComponent = msc.component;
        msc.binding.ControlObject = msc.bindobject;
        msc.binding.ControlDelegateName = msc.delegatename;

        //Update the serializedproperties
        msc.binding.SerialBinding.FindPropertyRelative("ControlComponent").objectReferenceValue = msc.component;
        msc.binding.SerialBinding.FindPropertyRelative("ControlObject").objectReferenceValue = msc.bindobject;
        msc.binding.SerialBinding.FindPropertyRelative("ControlDelegateName").stringValue = msc.delegatename;
    }

    /// <summary>
    /// Returns the simple name of common types, so you can print "float" instead of "Single". 
    /// No way I know of to get this from C# so I'm just converting it by hand. Probably incomplete. 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    static string ConvertToSimpleName(Type type)
    {
        switch(type.Name)
        {
            case "Single":
                return "float";
            case "Boolean":
                return "bool";
            case "Int32":
            case "Int16":
                return "int";
            default:
                return type.Name;
        }
    }

    struct MenuSelectComponent
    {
        public EZConsoleBindingEditor binding;
        public GameObject bindobject;
        public Component component;
        public string delegatename;
    }
}

/// <summary>
/// Holds non-serialized references alongside the serialized property for simpler access. 
/// It can't be passed directly to the Console object because it's not fully serializable. It'll work but won't save.
/// I'm sure I remove this with simple but lengthy changes - going to do other cleanup first. 
/// Another idea is to make the first three properties into getters/setters that access the properties on SerialBinding. 
/// </summary>
public class EZConsoleBindingEditor
{
    //public MethodInfo TargetMethod; //The method on the target script we'll bind to //Now in a dictionary. 
    public GameObject ControlObject; //The gameobject that has the delegate we'll bind to the method
    public Component ControlComponent; //The specific component with the delegate we'll bind
    public string ControlDelegateName; //The name of the delegate we'll bind. Using a string so we can call reflection later. 
    public SerializedProperty SerialBinding; //The equivalent EZConsoleBindingSerial so we can update properties in real time. 
}

/// <summary>
/// Holds bindings between a target method and the delegates that will invoke it. 
/// Limited in functionality because it needs to be serializable. 
/// </summary>
[Serializable]
public class EZConsoleBindingSerial
{
    [SerializeField]
    public string TargetMethodName; //Use GetMethod to interpret
    [SerializeField]
    public GameObject ControlObject;
    [SerializeField]
    public Component ControlComponent; //Reference to the controlling component
    [SerializeField]
    public string ControlDelegateName; //Use GetField and cast with Expression.GetActionType or GetFunctionType to interpret
}