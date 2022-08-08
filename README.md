# ValueProxyExtensions

A [NeosModLoader](https://github.com/zkxs/NeosModLoader) mod for [Neos VR](https://neos.com/) that adds a few extra features to reference and value proxies.<br>
All features can be individually disabled using the config system.
- Reference Proxies now display the type they are storing, and also act as a value proxy with the full type name (useful for dropping into the component attacher generic type selector)
- Value Proxies can be clicked into a field similar to reference proxies
- Pressing secondary with a logix tip while holding a value proxy will create an input with that value
- LogiX displays allow grabbing their contained value with an extra generated button
- Inspector panels will generate buttons that allow grabbing and dropping types such as float3, floatQ, enums etc.
- Value Proxies can be transferred between world and userspace by using the click in feature

Known Issue:
If you are using the [LogixUtils](https://github.com/badhaloninja/LogixUtils) mod setting the logix tip to `extract: reference node` will prevent you from creating value inputs from value proxies.

## Installation
1. Install [NeosModLoader](https://github.com/zkxs/NeosModLoader).
1. Place [ValueProxyExtensions.dll](https://github.com/art0007i/ValueProxyExtensions/releases/latest/download/ValueProxyExtensions.dll) into your `nml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\NeosVR\nml_mods` for a default install. You can create it if it's missing, or if you launch the game once with NeosModLoader installed it will create the folder for you.
1. Start the game. If you want to verify that the mod is working you can check your Neos logs.
