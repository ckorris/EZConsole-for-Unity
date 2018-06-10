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
public class ConsoleV3 : MonoBehaviour
{
    //TODO: Rename to ScriptType and ScriptInstance along with relevant methods like ChangeRuntimeScript(). 
    [SerializeField]
    public MonoScript CompileTimeScript; //For compile-time, and briefly at Start for reflection purposes. 
    [SerializeField]
    public Component RuntimeScript; //The script attached to the gameobject from which we actually create the delegates. 

    [SerializeField]
    public List<EZConsoleBindingSerial> Bindings;
    /*[SerializeField]
    public List<EZConsoleBindingSerial> Bindings
    {
        get
        {
            return _bindings;
        }
        set
        {
            _bindings = value;
        }
    }*/

    //Testing serialization
    public List<EZConsoleBindingSerial> TestBindings;
    public EZConsoleBindingSerial SingleSerial;

    MethodInfo _registerMethod;

    // Use this for initialization
    void Start()
    {
        //Cache the method that registers a delegate. (May need to turn into list if we use the 32 brute force Func/Action thingy.)
        _registerMethod = typeof(ConsoleV3).GetMethod("RegisterDelegateHelper", BindingFlags.Instance | BindingFlags.NonPublic);

        SetUpBindings();
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

    public void SetBindingsFromEditor(List<EZConsoleBindingSerial> inputbinds) //Might not need this anymore. 
    {
        Bindings = new List<EZConsoleBindingSerial>();
        foreach (EZConsoleBindingSerial sbind in inputbinds)
        {
            Bindings.Add(sbind);
        }
    }

    /// <summary>
    /// Change runtime script during runtime. Handles unbinding from the old component and bindings to the new one. 
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


[CustomEditor(typeof(ConsoleV3))]
public class ConsoleV3Editor : Editor
{
    ConsoleV3 _console; //The console we're editing, for direct edits

    SerializedObject _consoleSerial; //The serialized object version for applying certain properties that don't like to serialize.
    SerializedProperty _compileScriptSerial;
    SerializedProperty _runtimeScriptSerial;
    SerializedProperty _bindingsSerial;

    //Test
    SerializedProperty _singleSerial;
    SerializedProperty _testBindings;

    //Shorthand
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

    //List<EZConsoleBindingEditor> _editorBindingsList;
    Dictionary<MethodInfo, List<EZConsoleBindingEditor>> _editorBindingsList;

    private void OnEnable()
    {
        _console = (ConsoleV3)target;
        _lastScript = _script;

        _consoleSerial = new SerializedObject(target);
        _compileScriptSerial = _consoleSerial.FindProperty("CompileTimeScript");
        _runtimeScriptSerial = _consoleSerial.FindProperty("RuntimeScript");
        _bindingsSerial = _consoleSerial.FindProperty("Bindings");

        //Test
        _singleSerial = _consoleSerial.FindProperty("SingleSerial");
        _testBindings = _consoleSerial.FindProperty("TestBindings");
    }

    public override void OnInspectorGUI()
    {
        GUILayoutOption[] layoutoptions = new GUILayoutOption[2]
        {
            GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth / 2),
            GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight)
        };

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
            //EditorGUILayout.LabelField("Runtime Component: ", EditorStyles.boldLabel);
            Component oldcomponent = _console.RuntimeScript; //Cache this so we can run ChangeRuntimeScript if needed
            EditorGUILayout.ObjectField(_runtimeScriptSerial, _scriptType, GUIContent.none, layoutoptions);
            _consoleSerial.ApplyModifiedProperties();
            if (Application.isPlaying && oldcomponent != _console.RuntimeScript)
            {
                _console.ChangeRuntimeScript(oldcomponent, _console.RuntimeScript);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.Separator();

        //New list attempt. Let's see how this works. 
        if (_script != null)
        {
            //Make the dictionary of editorbindings, which represents the lists only in the editor.
            Dictionary<MethodInfo, List<EZConsoleBindingEditor>> editorbindings = new Dictionary<MethodInfo, List<EZConsoleBindingEditor>>();

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

                //Actually draw the list - after we add all the callbacks. 
                ReorderableList reorderablelist = new ReorderableList(editorbindings[minfo], typeof(EZConsoleBindingEditor), true, true, true, true);

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

                    //Component/function dropdown menu
                    EditorGUI.BeginDisabledGroup(ebind.ControlObject == null);
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

                };

                reorderablelist.index = reorderablelist.count - 1; //We need this for the removal button to work, because the index isn't set in any sane or observable way in Unity. 
                reorderablelist.DoLayoutList();
            }

            _consoleSerial.ApplyModifiedProperties();
        }
    }

    //public override void OnInspectorGUI()
    public void OldOnInspectorGUI() //Remember to put the override if you restore this
    {
        //DrawDefaultInspector();

        //Test
        SerializedProperty singleMethodName = _singleSerial.FindPropertyRelative("TargetMethodName");
        EditorGUILayout.PropertyField(singleMethodName, new GUILayoutOption[0]);
        //singleMethodName.serializedObject.Update();
        _consoleSerial.ApplyModifiedProperties();

        ReorderableList testlist = new ReorderableList(_consoleSerial, _testBindings, true, true, true, true);
        testlist.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            TestDrawBind(testlist, rect, index, isActive, isFocused);
        };

        testlist.DoLayoutList();

        /*if (_console.Bindings != null) //This lets you load between selections but won't you actually update the Console list. 
        {
            //_editorBindingsList = MakeEditorBindingList(_console.Bindings);
        }*/

        //Target monoscript (compile-time script) object field
        //Don't allow changing the target monoscript during runtime
        EditorGUI.BeginDisabledGroup(Application.isPlaying);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Target Monoscript: ", EditorStyles.boldLabel);
        //_script = (MonoScript)EditorGUILayout.ObjectField(_script, typeof(MonoScript), false, null);
        EditorGUILayout.ObjectField(_compileScriptSerial, typeof(MonoScript), GUIContent.none, new GUILayoutOption[0]);
        _consoleSerial.ApplyModifiedProperties();
        EditorGUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();

        //Target component (runtime script) object field
        if (_script != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Runtime Component: ", EditorStyles.boldLabel);
            //_console.RuntimeScript = (Component)EditorGUILayout.ObjectField(_console.RuntimeScript, _scriptType, true, null);
            Component oldcomponent = _console.RuntimeScript; //Cache this so we can run ChangeRuntimeScript if needed
            EditorGUILayout.ObjectField(_runtimeScriptSerial, _scriptType, GUIContent.none, null);
            _consoleSerial.ApplyModifiedProperties();
            if (Application.isPlaying && oldcomponent != _console.RuntimeScript)
            {
                _console.ChangeRuntimeScript(oldcomponent, _console.RuntimeScript);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.Separator();

        if (_editorBindingsList == null || _script != _lastScript)
        {
            if (_script != null) //There's a new script, so simply update it
            {
                _editorBindingsList = MakeEditorBindingList(_console.Bindings);
            }
            else //The new script is empty, so just clear everything. 
            {
                _console.SetBindingsFromEditor(new List<EZConsoleBindingSerial>());

                _editorBindingsList = new Dictionary<MethodInfo, List<EZConsoleBindingEditor>>();
            }

            _lastScript = _script;
        }

        UpdateBindingsList();

        //List all methods in target script
        if (_script != null)
        {

            //Go through each method and draw the list of bindings. 
            foreach (MethodInfo minfo in _editorBindingsList.Keys)
            {
                //More tests
                //if (_editorBindingsList.Keys.ElementAt(0) != minfo) continue;

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

                //Draw the list of EZConsoleBindingEditors as a reorderable list and add proper callbacks
                ReorderableList reorderablelist = new ReorderableList(_editorBindingsList[minfo], typeof(EZConsoleBindingEditor), true, true, true, true);

                //Add label to the header
                reorderablelist.drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, minfo.Name, EditorStyles.boldLabel);
                };

                reorderablelist.onCanRemoveCallback = (ReorderableList rlist) =>
                {
                    //Debug.Log("Index: "  + rlist.index + " Count: " + rlist.count + "Can call: " + rlist.onCanRemoveCallback.Invoke(rlist));
                    return rlist.count > 0;
                };

                //We need to subscribe a draw function to drawElementCallback, but I can't pass the list to it. Or can I? 
                reorderablelist.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    //SCREW THE RULES I'VE GOT LAMBDA
                    DrawBindingInList(_editorBindingsList[minfo], minfo, deltype, rect, index, isActive, isFocused);
                };


                reorderablelist.index = reorderablelist.count - 1;
                reorderablelist.DoLayoutList();

            }

            //_console.SetBindingsFromEditor(MakeSerializableBindingList(_editorBindingsList));

            _console.Bindings = MakeSerializableBindingList(_editorBindingsList);
            _bindingsSerial.serializedObject.Update();

        }
    }

    public void TestDrawBind(ReorderableList list, Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
        EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight),
        element.FindPropertyRelative("TargetMethodName"), GUIContent.none);
    }

    public void DrawBindingInList(List<EZConsoleBindingEditor> list, MethodInfo minfo, Type deltype, Rect rect, int index, bool isActive, bool isFocused)
    {
        EditorGUI.LabelField(rect, index.ToString());

        //Draw the box
        EditorGUILayout.BeginHorizontal();


        Rect controlobjectrect = new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight);
        _editorBindingsList[minfo][index].ControlObject = (GameObject)EditorGUI.ObjectField(controlobjectrect, _editorBindingsList[minfo][index].ControlObject, typeof(UnityEngine.Object), true);
        EditorGUI.BeginDisabledGroup(_editorBindingsList[minfo][index].ControlObject == null);

        //Give a name to the label
        string emptylabel;
        if (_editorBindingsList[minfo][index].ControlDelegateName == "" || _editorBindingsList[minfo][index].ControlDelegateName == null)
        {
            emptylabel = "No Function";
        }
        else
        {
            emptylabel = _editorBindingsList[minfo][index].ControlDelegateName;
        }
        GUIContent dropdowncontent = new GUIContent(emptylabel);

        Rect dropdownrect = new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight);
        if (EditorGUI.DropdownButton(dropdownrect, dropdowncontent, FocusType.Keyboard))
        {
            GenericMenu menu = new GenericMenu();

            //Add the "No Component" option
            MenuSelectComponent emptymsc = new MenuSelectComponent()
            {
                binding = _editorBindingsList[minfo][index],
                bindobject = _editorBindingsList[minfo][index].ControlObject,
                component = null
            };
            menu.AddItem(new GUIContent("No Component"), _editorBindingsList[minfo][index].ControlComponent == null, SelectFunction, emptymsc);

            menu.AddSeparator("");

            //List all components
            Component[] components = _editorBindingsList[minfo][index].ControlObject.GetComponents<Component>();
            for (int j = 0; j < components.Length; j++)
            {
                //List all delegates in the control, which includes actions, functions, etc. 
                List<FieldInfo> fieldinfos = components[j].GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(t => t.FieldType.IsAssignableFrom(deltype))
                    .ToList();


                //Debug.Log(meminfos.Count);
                foreach (FieldInfo t in fieldinfos)
                {
                    MenuSelectComponent msc = new MenuSelectComponent()
                    {
                        binding = _editorBindingsList[minfo][index],
                        component = components[j],
                        bindobject = _editorBindingsList[minfo][index].ControlObject,
                        delegatename = t.Name
                    };

                    string path = components[j].GetType().Name + "/" + t.Name;
                    menu.AddItem(new GUIContent(path), _editorBindingsList[minfo][index].ControlComponent == components[j], SelectFunction, msc);

                }
            }


            menu.ShowAsContext();
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
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

        //Update the ebinding (probable unnecessary - try cleaning later)
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

    /// <summary>
    /// Checks the list of bindings against what methods the script has. This preserves old but valid bindings while removing ones that are no longer needed. 
    /// </summary>
    private void UpdateBindingsList()
    {
        if (_script != null)
        {
            Dictionary<MethodInfo, List<EZConsoleBindingEditor>> newbindings = new Dictionary<MethodInfo, List<EZConsoleBindingEditor>>();

            ConsoleV3 targetconsole = (ConsoleV3)target;
            MethodInfo[] methods = _scriptType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => !x.DeclaringType.IsAssignableFrom(typeof(MonoBehaviour)))
                .ToArray();

            for (int i = 0; i < methods.Length; i++) //Iterate through methodinfos from the script as of now. 
            {
                if (_editorBindingsList.ContainsKey(methods[i])) //The editorbindingslist had a list of bindings, so use that one. 
                {
                    newbindings.Add(methods[i], _editorBindingsList[methods[i]]);
                }
                else //The binding list doesn't have that method, so add a new empty one. 
                {
                    newbindings.Add(methods[i], new List<EZConsoleBindingEditor>());
                }
            }

            _editorBindingsList = newbindings;
        }
    }

    /// <summary>
    /// Since EZConsoleEditorBindings is more useful but not serializable, this turns a list of them into the serializable EZConsoleBinding
    /// </summary>
    /// <param name="editorbindings"></param>
    /// <returns></returns>
    public List<EZConsoleBindingSerial> MakeSerializableBindingList(Dictionary<MethodInfo, List<EZConsoleBindingEditor>> editorbindings)
    {
        List<EZConsoleBindingSerial> sbindings = new List<EZConsoleBindingSerial>();

        foreach (MethodInfo minfokey in editorbindings.Keys)
        {
            foreach (EZConsoleBindingEditor ebind in editorbindings[minfokey])
            {
                EZConsoleBindingSerial sbind = new EZConsoleBindingSerial()
                {
                    TargetMethodName = minfokey.Name,
                    ControlObject = ebind.ControlObject,
                    ControlComponent = ebind.ControlComponent, //SHOOOULD be serializable but we'll see. 
                    ControlDelegateName = ebind.ControlDelegateName
                };

                sbindings.Add(sbind);
            }
        }

        return sbindings;
    }

    /// <summary>
    /// Turns the serializable bindings list from the console into the more useful EZConsoleBindingEditor. 
    /// </summary>
    /// <param name="serialbindings"></param>
    /// <returns></returns>
    public Dictionary<MethodInfo, List<EZConsoleBindingEditor>> MakeEditorBindingList(List<EZConsoleBindingSerial> serialbindings)
    {
        //List<EZConsoleBindingEditor> ebindings = new List<EZConsoleBindingEditor>();
        Dictionary<MethodInfo, List<EZConsoleBindingEditor>> ebindings = new Dictionary<MethodInfo, List<EZConsoleBindingEditor>>();

        foreach (EZConsoleBindingSerial sbind in serialbindings)
        {
            //If we don't already have a key for that methodinfo, add it. 
            //Iterating through the list of keys and checking for a name, because to check if the MethodInfo exists would result in needless reflection calls. 
            MethodInfo minfokey = ebindings.Keys.FirstOrDefault(m => m.Name == sbind.TargetMethodName);

            if (minfokey == null)
            {
                MethodInfo newminfo = _scriptType.GetMethod(sbind.TargetMethodName);
                ebindings.Add(newminfo, new List<EZConsoleBindingEditor>());
                minfokey = newminfo; //So that we can reference it to add the binding without calling reflection again.
            }

            EZConsoleBindingEditor ebind = new EZConsoleBindingEditor()
            {
                //TargetMethod = _scriptType.GetMethod(sbind.TargetMethodName),
                ControlObject = sbind.ControlObject != null ? sbind.ControlObject : null,
                ControlComponent = sbind.ControlComponent != null ? sbind.ControlComponent : null,
                ControlDelegateName = sbind.ControlDelegateName
            };

            ebindings[minfokey].Add(ebind);
        }

        return ebindings;
    }
}


/// <summary>
/// In the editor, holds bindings between the target script and the objects that connect to the methods, in the editor.
/// Note that it's not fully serializable, so it can't be passed directly to the Console object. 
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
/// Limited in functionality because it needs to be serializable - The editor and console will need to use reflection to use. 
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