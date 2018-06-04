using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using UnityEngine;
using UnityEditor;
using System.Linq.Expressions;

public class ConsoleV3 : MonoBehaviour
{
    public MonoScript CompileTimeScript; //For compile-time, and briefly at Start for reflection purposes. 

    public Component RuntimeScript; //The script attached to the gameobject from which we actually create the delegates. 



    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

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

    static List<EZConsoleEditorBinding> _editorBindingsList;

    ConsoleV3Editor() //Constructor should be fine because this isn't a MonoBehaviour. We'll see. 
    {
        if (_editorBindingsList == null)
        {
            //UpdateBindingsList();
        }
    }

    private void OnEnable()
    {
        _console = (ConsoleV3)target;
        _lastScript = _script;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        UpdateBindingsList();

        //Test for now. List all methods in target script
        if (_script != null)
        {
            /*MethodInfo[] methods = _scriptType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => !x.DeclaringType.IsAssignableFrom(typeof(MonoBehaviour)))
                .ToArray();
                */

            for (int i = 0; i < _editorBindingsList.Count; i++)
            {

                EditorGUILayout.LabelField(_editorBindingsList[i].TargetMethod.Name, EditorStyles.boldLabel); //Title

                //Find the parameters and return type of the target method
                Type returntype = _editorBindingsList[i].TargetMethod.ReturnType;
                ParameterInfo[] paraminfos = _editorBindingsList[i].TargetMethod.GetParameters();

                //Make an array of types that include parameter types, with one at the end that represents the return type. 
                //This will get used to find the delegate type we'll need, and follows the format required by Func. 
                Type[] paramtypes = new Type[paraminfos.Length];
                for(int p = 0; p < paraminfos.Length; p++)
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

                //Debug.Log(_editorBindingsList[i].TargetMethod + " needs a delegate of type " + deltype);

                //Draw the box
                EditorGUILayout.BeginHorizontal();

                _editorBindingsList[i].ControlObject = (GameObject)EditorGUILayout.ObjectField(_editorBindingsList[i].ControlObject, typeof(UnityEngine.Object), true);
                EditorGUI.BeginDisabledGroup(_editorBindingsList[i].ControlObject == null);
                GUIContent dropdowncontent = new GUIContent(_editorBindingsList[i].ControlDelegateName != null ? _editorBindingsList[i].ControlDelegateName : "No Function");
                if (EditorGUILayout.DropdownButton(dropdowncontent, FocusType.Keyboard))
                {
                    GenericMenu menu = new GenericMenu();

                    //Add the "No Component" option
                    MenuSelectComponent emptymsc = new MenuSelectComponent()
                    {
                        binding = _editorBindingsList[i],
                        component = null
                    };
                    menu.AddItem(new GUIContent("No Component"), _editorBindingsList[i].ControlComponent == null, SelectFunction, emptymsc);

                    menu.AddSeparator("");

                    //List all components
                    Component[] components = _editorBindingsList[i].ControlObject.GetComponents<Component>();
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
                                binding = _editorBindingsList[i],
                                component = components[j],
                                delegatename = t.Name
                            };

                            string path = components[j].GetType().Name + "/" + t.Name;
                            menu.AddItem(new GUIContent(path), _editorBindingsList[i].ControlComponent == components[j], SelectFunction, msc);

                        }
                    }
                                           

                    menu.ShowAsContext();
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

            }
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

        msc.binding.ControlComponent = msc.component;
        msc.binding.ControlDelegateName = msc.delegatename;
    }

    struct MenuSelectComponent
    {
        public EZConsoleEditorBinding binding;
        public Component component;
        public string delegatename;
    }

    private void UpdateBindingsList()
    {
        //Debug.Log(_editorBindingsList == null ? "Bindings List is null" : _editorBindingsList.Count.ToString());

        if (_script != null)
        {
            List<EZConsoleEditorBinding> newbindings = new List<EZConsoleEditorBinding>();

            ConsoleV3 targetconsole = (ConsoleV3)target;
            MethodInfo[] methods = _scriptType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => !x.DeclaringType.IsAssignableFrom(typeof(MonoBehaviour)))
                .ToArray();

            for (int i = 0; i < methods.Length; i++)
            {
                //If the binding exists in the list, add that one instead. 
                if (_editorBindingsList != null)
                {
                    bool foundoldbinding = false;
                    foreach (EZConsoleEditorBinding oldbinding in _editorBindingsList)
                    {
                        if (oldbinding.TargetMethod.Name == methods[i].Name)
                        {
                            //We found an old one. Add that one and not the new one. 
                            newbindings.Add(oldbinding);
                            foundoldbinding = true;
                            break; //Don't bother going further
                        }
                    }

                    if (foundoldbinding)
                    {
                        continue;
                    }
                }

                //If we didn't find an old one, make a new one. 
                EZConsoleEditorBinding binding = new EZConsoleEditorBinding();
                binding.TargetMethod = methods[i];

                newbindings.Add(binding);
            }

            _editorBindingsList = newbindings;
        }
    }
}

/// <summary>
/// In the editor, holds bindings between the target script and the objects that connect to the methods, in the editor.
/// Note that it's not fully serializable, so it can't be passed directly to the Console object. 
/// </summary>
[Serializable]
public class EZConsoleEditorBinding
{
    public MethodInfo TargetMethod; //The method on the target script we'll bind to
    public GameObject ControlObject; //The gameobject that has the delegate we'll bind to the method
    public Component ControlComponent; //The specific component with the delegate we'll bind
    public string ControlDelegateName; //The name of the delegate we'll bind. Using a string so we can call reflection later. 

}