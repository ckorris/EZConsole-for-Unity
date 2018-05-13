using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class ConsoleControl : MonoBehaviour
{
    public abstract Type ControlType
    {
        get;
    }

    public abstract void DeregisterPart(PartConsole console);
}
