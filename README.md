# EZConsole-for-Unity
A quick way for prototyping controls and displays for any Unity script. 

The Console script can be given the type of any component that inherits from Monobehaviour. Once you do, it lists out all the getters, setters and methods in the Inspector. You simply drag and drop control and display objects into those slots, and then you can easily control the values and print them out. 

For example: Say you had a Car script you wrote in Unity. You'd create a Console, and drag-and-drop the Car script from Assets into its Target Monoscript value. Then you'll see a list of things you can display, such as speed, RPM, gas remaining, throttle, turning direction, etc. You'd also have things you could control, like throttle, turning direction, etc. You can then plug in a display and control object for each. Aside from a few restrictions, you can display ALL properties with a getter, and control ALL properties with a setter or launch any methods with 0 or 1 parameters. That includes custom types. 

Then you can attach that Console to ANY instantiated Car object, including at runtime, and the displays/controls will just work.  This is designed to work well for prefabs; you could make a single Car dashboard, then put it in many cars at runtime and they'll all work. 

The only work you have to do is make the displays/controls yourself, but it's desiged so that all controls you make are easily reusable. Simply make a new script that inherits from BaseDisplay<T> or BaseControl<T>, where T is the type of value you want to change. For instance, a gas pedal might inherit BaseControl<float>. Then make your logic call the virtual methods UpdateValue() or ActivateAction().  

Once you make it, you can assign it to any console that takes that value. For instance, it would take 10 seconds to put that gas pedal on a spaceship, a gun, a scrollbar, etc. 

Limitations: 
-Currently does not support displaying/modifying values that aren't properties
-Currently does not support calling methods with more than one parameter (because BaseControl<T> has one type)

To do:
-Remove the above limitations
-Add example BaseDisplays and BaseControls, as currently there's only two really crappy ones. 
