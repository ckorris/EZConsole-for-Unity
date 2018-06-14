using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Retrieves a float value and updates a text mesh with that number. 
/// As it's done with generics, that < float > is listerally all you need to make this work. 
/// If you want to change how the value is displayed, override UpdateText. 
/// </summary>
public class TextDisplayFloat : TextDisplay<float> { }
