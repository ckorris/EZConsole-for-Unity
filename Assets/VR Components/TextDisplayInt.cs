using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Retrieves an int value and updates a text mesh with that number. 
/// As it's done with generics, that < int > is listerally all you need to make this work. 
/// /// If you want to change how the value is displayed, override UpdateText. 
/// </summary>
public class TextDisplayInt : TextDisplay<int> { }
