using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Retrieves a bool value and updates a text mesh with it. 
/// As it's done with generics, that < bool > is listerally all you need to make this work. 
/// /// If you want to change how the value is displayed, override UpdateText. 
/// That would be a good idea because "true" and "false" look kinda plain in a game. 
/// </summary>
public class TextDisplayBool : TextDisplay<bool> { }