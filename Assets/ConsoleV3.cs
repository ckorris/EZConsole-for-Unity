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
    public MonoScript CompileTimeScript; //For compile-time, and briefly at Start for reflection purposes. 

    public Component RuntimeScript; //The script attached to the gameobject from which we actually create the delegates. 

    [SerializeField]
    private List<EZConsoleBindingSerial> _bindings = new List<EZConsoleBindingSerial>();
    [SerializeField]
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
    }

    // Use this for initialization
    void Start()
    {

    }

    private void SetUpBindings()
    {
        //TODO: Store methodinfos in a dictionary with the name as a key so multiple delegates on one method only need one reflection call. 
        foreach(EZConsoleBindingSerial binding in _bindings)
        {
            MethodInfo minfo = CompileTimeScript.GetClass().GetMethod(binding.TargetMethodName); //The method to register

            FieldInfo finfo = binding.ControlComponent.GetType().GetField(binding.ControlDelegateName); //The delegate to add the method to
            
            //TODO: Try replicating old register methods, but pass the whole delegate type as T instead of the delegate argument type. 
        }
    }

    public void SetBindings(List<EZConsoleBindingSerial> inputbinds) //Might not need this anymore. 
    {
        _bindings = new List<EZConsoleBindingSerial>();
        foreach (EZConsoleBindingSerial sbind in inputbinds)
        {
            Bindings.Add(sbind);
        }
    }
}


[CustomEditor(typeof(ConsoleV3))]
public class ConsoleV3Editor : Editor
{
    ConsoleV3 _console; //The console we're editing

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
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        /*if (_console.Bindings != null) //This lets you load between selections but won't you actually update the Console list. 
        {
            //_editorBindingsList = MakeEditorBindingList(_console.Bindings);
        }*/

        if (_editorBindingsList == null || _script != _lastScript)
        {
            if (_script != null) //There's a new script, so simply update it
            {
                _editorBindingsList = MakeEditorBindingList(_console.Bindings);
            }
            else //The new script is empty, so just clear everything. 
            {
                _console.SetBindings(new List<EZConsoleBindingSerial>());

                _editorBindingsList = new Dictionary<MethodInfo, List<EZConsoleBindingEditor>>();
            }

            _lastScript = _script;
        }

        UpdateBindingsList();

        //Test for now. List all methods in target script
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

            _console.SetBindings(MakeSerializableBindingList(_editorBindingsList));

        }
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

        msc.binding.ControlComponent = msc.component;
        msc.binding.ControlObject = msc.bindobject;
        msc.binding.ControlDelegateName = msc.delegatename;
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