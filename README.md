# ValueProxyExtensions

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/) that adds a few extra features to reference and value proxies.<br>
All features can be individually disabled using the config system.
- Reference Proxies now display the type they are storing, and also act as a value proxy with the full type name (useful for dropping into the component attacher generic type selector) [#647](https://github.com/Yellow-Dog-Man/Resonite-Issues/issues/647)
- Value Proxies can be clicked into a field similar to reference proxies [#648](https://github.com/Yellow-Dog-Man/Resonite-Issues/issues/648)
- Pressing secondary with a flux tip while holding a value or reference proxy will create an input with that value [#645](https://github.com/Yellow-Dog-Man/Resonite-Issues/issues/645)
- Inspector panels will generate buttons that allow grabbing and dropping types such as float3, floatQ, enums etc. (disabled by default as a similar feature has been added to resonite)
- Value Proxies can be transferred between world and userspace by using the click in feature.

## Installation
1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
1. Place [ValueProxyExtensions.dll](https://github.com/art0007i/ValueProxyExtensions/releases/latest/download/ValueProxyExtensions.dll) into your `rml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a default install. You can create it if it's missing, or if you launch the game once with ResoniteModLoader installed it will create the folder for you.
1. Start the game. If you want to verify that the mod is working you can check your Resonite logs.
