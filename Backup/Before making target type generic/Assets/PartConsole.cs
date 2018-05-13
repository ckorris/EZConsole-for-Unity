using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using UnityEngine.Events;

[Serializable]
public class PartConsole : MonoBehaviour
{
    public SomePart TargetPart;

    /// <summary>
    /// The type of the target part, which is currently just GetType() of the target object but should later connect to a script. 
    /// </summary>
    public Type PartType
    {
        get
        {
            if (TargetPart)
            {
                return TargetPart.GetType();
            }
            else return null;
        }
    }

    [SerializeField]
    private List<DisplayPropBinding> _displayBindings = new List<DisplayPropBinding>();
    public List<DisplayPropBinding> DisplayBindings
    {
        get
        {
            return _displayBindings;
        }
        set
        {
            _displayBindings = value;
        }
    }

    [SerializeField]
    private List<ControlPropBinding> _controlBindings = new List<ControlPropBinding>();
    public List<ControlPropBinding> ControlBindings
    {
        get
        {
            return _controlBindings;
        }
        set
        {
            _controlBindings = value;
        }
    }

    public float FloatTest = 4.2f;
    public float SecondFloatTest = 8.12f;

    private void Awake()
    {
        BindToReflectedProperties();
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        foreach (DisplayPropBinding binding in DisplayBindings)
        {
            if (binding.Display != null)
            {
                binding.Display.UpdateValue(binding.Info.GetValue(TargetPart, null));
            }
        }
    }

    /// <summary>
    /// Takes the names, types etc. in DisplayBindings and uses reflection to find a PropertyInfo for them. 
    /// Only call on start, or when something has been changed in the editor (NOT every frame) as it's heavy. 
    /// </summary>
    public void BindToReflectedProperties()
    {
        //Call reflection once to get references to the properties for the displayers that aren't heavy later. 
        for (int i = 0; i < _displayBindings.Count; i++)
        {
            //We have to replace the struct with a new one because we're stuck with structs because serialization. 
            PropertyInfo newinfo = PartType.GetProperty(_displayBindings[i].PropertyName);

            DisplayPropBinding newbinding = new DisplayPropBinding
            {
                PropertyName = _displayBindings[i].PropertyName,
                Display = _displayBindings[i].Display,
                PropertyTypeString = _displayBindings[i].PropertyTypeString,
                Info = newinfo
            };
            _displayBindings[i] = newbinding;
            
        }

        //Now call reflection for the control properties
        //ClearControlActionsByPart();
        //Note that if this isn't done at start, you should've called ClearControlActionsByPart first so you don't get duplicates or leave actions on parts no longer bound. 

        for (int i = 0; i < _controlBindings.Count; i++)
        {
            //We have to replace the struct with a new one because we're stuck with structs because serialization. 
            PropertyInfo newinfo = PartType.GetProperty(_controlBindings[i].PropertyName);

            ControlPropBinding newbinding = new ControlPropBinding
            {
                PropertyName = _controlBindings[i].PropertyName,
                Control = _controlBindings[i].Control,
                PropertyTypeString = _controlBindings[i].PropertyTypeString,
                Info = newinfo
            };
            _controlBindings[i] = newbinding;

            //Create and assign an action for setting the property to the control, if there is one. 
            //TODO: Clean up past assignments. 
            if (newbinding.Control) //
            {
                AssignSetterToControl(newbinding);
            }
        }
    }

    void AssignSetterToControl(ControlPropBinding binding)
    {
        string controltypestring = GetControlType(binding.PropertyTypeString).ToString(); //Yes, casting a type to a string to a type to a string to a type. But we need the base type. 
        switch (controltypestring) 
        {
            case "BoolConsoleControl":
                BoolConsoleControl boolcontrol = binding.Control as BoolConsoleControl;
                Action<bool> boolaction = new Action<bool>(x => binding.Info.SetValue(TargetPart, x, null));
                boolcontrol.RegisterAction(this, boolaction);
                //BoolConsoleControl boolcontrol = binding.Control as BoolConsoleControl;
                //boolcontrol.ActivateBool.AddListener(x => binding.Info.SetValue(TargetPart, x, null));
                break;
            case "FloatConsoleControl":
                FloatConsoleControl floatcontrol = binding.Control as FloatConsoleControl;
                Action<float> floataction = new Action<float>(x => binding.Info.SetValue(TargetPart, x, null));
                floatcontrol.RegisterAction(this, floataction);
                //floatcontrol.ActivateFloat.AddListener(new UnityEngine.Events.UnityAction<float>(x => binding.Info.SetValue(TargetPart, x, null)));
                //floatcontrol.ActivateFloat.AddListener(x => binding.Info.SetValue(TargetPart, x, null));
                break;
            case "Vector3ConsoleControl":
                Vector3ConsoleControl vectorcontrol = binding.Control as Vector3ConsoleControl;
                Action<Vector3> vectoraction = new Action<Vector3>(x => binding.Info.SetValue(TargetPart, x, null));
                vectorcontrol.RegisterAction(this, vectoraction);
                //Vector3ConsoleControl vectorcontrol = binding.Control as Vector3ConsoleControl;
                //vectorcontrol.ActivateVectorThree.AddListener(x => binding.Info.SetValue(TargetPart, x, null));
                break;
            default:
                print("Tried to assign control of type " + controltypestring + " which isn't supported in " + name + ".");
                break;
        }
    }

    //TODO: Add function to clear controls of all actions tied to TargetPart, to be called by the custom inspector. 
    /// <summary>
    /// Goes through all control bindings, and for each control bound to something in the target part, tell it to remove that action. 
    /// Useful if you change the target part, or if you're changing stuff at runtime and need to redo reflection
    /// TODO: Call methods, not just lambda property actions
    /// </summary>
    /// <param name="part"></param>
    public void ClearControlActionsByPart()
    {
        foreach(ControlPropBinding binding in ControlBindings)
        {
            if(binding.Control)
            {
                binding.Control.DeregisterPart(this);
            }
        }
    }

    public static Type GetDisplayType(string typestring)
    {
        //Send a System type into this method to get the display class that represents it. Will be inherited from ConsoleDisplay. 
        switch (typestring)
        {
            case "System.Boolean":
                return typeof(BoolConsoleDisplay);
            case "System.Single":
            case "System.Int32":
            case "System.Double":
                return typeof(FloatConsoleDisplay);
            case "UnityEngine.Vector3":
                return typeof(Vector3ConsoleDisplay);
            default:
                return null;
        }
    }

    public static Type GetControlType(string typestring)
    {
        //For setters, not methods. Send a System type into this method to get the display class that represents it. Will be inherited from ConsoleDisplay. 
        switch (typestring)
        {
            case "System.Boolean":
                return typeof(BoolConsoleControl);
            case "System.Single":
            case "System.Int32":
            case "System.Double":
                return typeof(FloatConsoleControl);
            case "UnityEngine.Vector3":
                return typeof(Vector3ConsoleControl);
            default:
                return null;
        }
    }
}


[CustomEditor(typeof(PartConsole))]
public class PartConsoleEditor : Editor
{

    PartConsole _partConsole;
    SomePart _partTarget
    {
        get
        {
            return _partConsole.TargetPart;
        }

    }

    List<PropertyInfo> disppropinfolist;
    List<PropertyInfo> contpropinfolist;

    //Store the last Part so we can know if it's changed
    SomePart _lastPartTarget;

    private void OnEnable()
    {
        _partConsole = (PartConsole)target;
        _lastPartTarget = _partTarget;

        UpdatePropertyList();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (_partTarget != _lastPartTarget)
        {
            _partConsole.ClearControlActionsByPart();
            UpdatePropertyList();
        }

        bool updateruntimebindings = false; //If true, will call BindToReflectedProperties() at the end of the method

        //Display properties
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Display Properties", EditorStyles.boldLabel);
        for (int i = 0; i < _partConsole.DisplayBindings.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(_partConsole.DisplayBindings[i].PropertyName);

            DisplayPropBinding tempbinding = new DisplayPropBinding
            {
                PropertyName = _partConsole.DisplayBindings[i].PropertyName,
                Display = EditorGUILayout.ObjectField(_partConsole.DisplayBindings[i].Display, PartConsole.GetDisplayType(_partConsole.DisplayBindings[i].PropertyTypeString), true) as ConsoleDisplay,
                PropertyTypeString = _partConsole.DisplayBindings[i].PropertyTypeString
            };
            EditorGUILayout.EndHorizontal();

            if (tempbinding.Display != _partConsole.DisplayBindings[i].Display)
            {
                _partConsole.DisplayBindings[i] = tempbinding;
                if (Application.isPlaying)
                {
                    //_partConsole.BindToReflectedProperties();
                    updateruntimebindings = true;
                }
            }
        };

        //Control properties
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Control Properties", EditorStyles.boldLabel);
        for (int i = 0; i < _partConsole.ControlBindings.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(_partConsole.ControlBindings[i].PropertyName);

            ControlPropBinding tempbinding = new ControlPropBinding
            {
                PropertyName = _partConsole.ControlBindings[i].PropertyName,
                Control = EditorGUILayout.ObjectField(_partConsole.ControlBindings[i].Control, PartConsole.GetControlType(_partConsole.ControlBindings[i].PropertyTypeString), true) as ConsoleControl,
                PropertyTypeString = _partConsole.ControlBindings[i].PropertyTypeString
            };

            EditorGUILayout.EndHorizontal();

            if (tempbinding.Control != _partConsole.ControlBindings[i].Control)
            {
                if (_partConsole.ControlBindings[i].Control != null)
                {
                    _partConsole.ControlBindings[i].Control.DeregisterPart(_partConsole);
                }
                _partConsole.ControlBindings[i] = tempbinding;
                if (Application.isPlaying)
                {
                    updateruntimebindings = true;
                }
            }

        }

        
        serializedObject.ApplyModifiedProperties();
        if (updateruntimebindings)
        {
            _partConsole.ClearControlActionsByPart();
            _partConsole.BindToReflectedProperties();
        }


    }

    #region Old Serialization Shit
    /*/EditorGUILayout.PropertyField(_property);

    /SerializedProperty iterator = serializedObject.GetIterator();

    bool next = iterator.NextVisible(true);
    while(next)
    {
        if (iterator.propertyType == SerializedPropertyType.Float)
        {
            EditorGUILayout.PropertyField(iterator);
        }
        next = iterator.NextVisible(true);
    }
    */
    #endregion


    public void UpdatePropertyList()
    {
        Debug.Log("Updating property list");
        _lastPartTarget = _partTarget;

        //Where we hold the values before checking them against the actual console's values
        disppropinfolist = new List<PropertyInfo>(); //For properties that are readable
        contpropinfolist = new List<PropertyInfo>();//For properties that are writable

        Type parttype = _partConsole.PartType;
        if (parttype != null)
        {
            //You can't call reflection on null, so update with the blank lists

            PropertyInfo[] properties = parttype.GetProperties();
            foreach (PropertyInfo info in properties)
            {
                if (info.DeclaringType == typeof(SomePart) || info.DeclaringType.IsSubclassOf(typeof(SomePart))) //SomePart would be the base part class in Hyperfusion
                {
                    //Assign its getter to display properties, if there is one
                    if (info.CanRead)
                    {
                        disppropinfolist.Add(info);
                    }

                    //Assign its setter to control properties, if there is one
                    if (info.CanWrite)
                    {
                        contpropinfolist.Add(info);
                    }
                }
            }
        }

        //Preserve the old settings so they don't get wiped out if they're common to a new part. 
        //Doing this separately for readables and writables. Maybe there's a better way but I haven't thought of it yet. 
        List<DisplayPropBinding> newdispbindings = new List<DisplayPropBinding>();
        foreach (PropertyInfo info in disppropinfolist)
        {
            DisplayPropBinding binding = new DisplayPropBinding
            {
                PropertyName = info.Name,
                PropertyTypeString = info.PropertyType.ToString()
            };

            newdispbindings.Add(binding);
        }

        //Now combine the old and new lists, so we can carry over old but valid properties.
        //_partConsole.DisplayBindings = newbindings; 
        List<DisplayPropBinding> finaldispbindings = new List<DisplayPropBinding>();
        List<DisplayPropBinding> badnewdispbindings = new List<DisplayPropBinding>();
        foreach (DisplayPropBinding oldbinding in _partConsole.DisplayBindings)
        {
            foreach (DisplayPropBinding newbinding in newdispbindings)
            {
                if (newbinding.PropertyName == oldbinding.PropertyName)
                {
                    //Take the old one, not the new one
                    finaldispbindings.Add(oldbinding);
                    //Add to the bad list to avoid adding the new version
                    badnewdispbindings.Add(newbinding);
                }
            }
        }

        //Add the new ones that didn't have old counterparts added
        foreach (DisplayPropBinding newbinding in newdispbindings)
        {
            if (!badnewdispbindings.Contains(newbinding))
            {
                finaldispbindings.Add(newbinding);
            }
        }

        _partConsole.DisplayBindings = finaldispbindings;

        //Now writable properties. 
        List<ControlPropBinding> newcontbindings = new List<ControlPropBinding>();
        foreach (PropertyInfo info in contpropinfolist)
        {
            ControlPropBinding binding = new ControlPropBinding
            {
                PropertyName = info.Name,
                PropertyTypeString = info.PropertyType.ToString()
            };

            newcontbindings.Add(binding);
        }

        //Now combine the old and new lists, so we can carry over old but valid properties.
        //_partConsole.DisplayBindings = newbindings; 
        List<ControlPropBinding> finalcontbindings = new List<ControlPropBinding>();
        List<ControlPropBinding> badnewcontbindings = new List<ControlPropBinding>();
        foreach (ControlPropBinding oldbinding in _partConsole.ControlBindings)
        {
            foreach (ControlPropBinding newbinding in newcontbindings)
            {
                if (newbinding.PropertyName == oldbinding.PropertyName)
                {
                    //Take the old one, not the new one
                    finalcontbindings.Add(oldbinding);
                    //Add to the bad list to avoid adding the new version
                    badnewcontbindings.Add(newbinding);
                }
            }
        }

        //Add the new ones that didn't have old counterparts added
        foreach (ControlPropBinding newbinding in newcontbindings)
        {
            if (!badnewcontbindings.Contains(newbinding))
            {
                finalcontbindings.Add(newbinding);
            }
        }

        _partConsole.ControlBindings = finalcontbindings;


        serializedObject.ApplyModifiedProperties();

    }


}

/// <summary>
/// For binding readable properties to displays. 
/// </summary>
[Serializable]
public struct DisplayPropBinding
{
    public string PropertyName;
    public string PropertyTypeString; //Must be a string so we can serialize it
    public ConsoleDisplay Display; //The display component
    public PropertyInfo Info; //Name of the property to be set by reflection on start. NOT set in the inspector because it can't be serialized. 
}

/// <summary>
/// For binding the setters of writable properties to controls. 
/// </summary>
[Serializable]
public struct ControlPropBinding
{
    public string PropertyName;
    public string PropertyTypeString; //Must be a string so we can serialize it
    public ConsoleControl Control; //The control component
    public PropertyInfo Info; //Name of the property to be set by reflection on start. NOT set in the inspector because it can't be serialized. 
}