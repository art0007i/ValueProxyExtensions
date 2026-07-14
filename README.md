# ValueProxyExtensions
[![Thunderstore Badge](https://modding.resonite.net/assets/available-on-thunderstore.svg)](https://thunderstore.io/c/resonite/)

A [Resonite](https://resonite.com/) mod that adds a few extra features to reference and value proxies.

All features can be individually disabled using the config system.

- Reference Proxies now display the type they are storing [#647](https://github.com/Yellow-Dog-Man/Resonite-Issues/issues/647)
- Reference Proxies also act as a value proxy with the full type name (disabled by default, can be used for dropping into the component attacher generic type selector)
- Value Proxies can be clicked into a field similar to reference proxies [#648](https://github.com/Yellow-Dog-Man/Resonite-Issues/issues/648)
- Pressing secondary with a flux tip while holding a value or reference proxy will create an input with that value [#645](https://github.com/Yellow-Dog-Man/Resonite-Issues/issues/645)
- Inspector panels will generate buttons that allow grabbing and dropping types such as float3, floatQ, enums etc. (disabled by default as a similar feature has been added to resonite)
- Value Proxies can be transferred between world and userspace by using the click in feature.

## Installation (Manual)
1. Install [BepisLoader](https://github.com/ResoniteModding/BepisLoader) for Resonite.
2. Download the latest release ZIP file (e.g., `art0007i-ValueProxyExtensions-1.0.0.zip`) from the [Releases](https://github.com/art0007i/ValueProxyExtensions/releases) page.
3. Extract the ZIP and copy the `plugins` folder to your BepInEx folder in your Resonite installation directory:
   - **Default location:** `C:\Program Files (x86)\Steam\steamapps\common\Resonite\BepInEx\`
4. Start the game. If you want to verify that the mod is working you can check your BepInEx logs.
