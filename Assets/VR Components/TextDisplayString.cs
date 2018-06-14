using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Retrieves a string value and updates a text mesh with it. 
/// As it's done with generics, that < string > is listerally all you need to make this work. 
/// /// If you want to change how the value is displayed, override UpdateText. 
/// </summary>
public class TextDisplayString : TextDisplay<string> { }
