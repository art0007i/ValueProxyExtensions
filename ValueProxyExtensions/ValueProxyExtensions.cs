using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using FrooxEngine.UIX;
using Elements.Core;
using FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes;

namespace ValueProxyExtensions
{
    public class ValueProxyExtensions : ResoniteMod
    {
        public override string Name => "ValueProxyExtensions";
        public override string Author => "art0007i";
        public override string Version => "2.1.0";
        public override string Link => "https://github.com/art0007i/ValueProxyExtensions/";

        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> KEY_REF_VISUAL = new("reference_proxy_visuals", "Determines whether text containing ref type should be generated on reference proxies.", () => true);
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> KEY_REF_VALUE_PROXY = new("reference_proxy_value_proxy", "Determines whether reference proxies should act as value proxies with the full type name.", () => true);
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> KEY_PROXY_TRANSFER = new("proxy_transfer", "Determines whether value proxies should be allowed to be transported to userspace and back.", () => true);
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> KEY_CREATE_INPUTS = new("create_inputs", "Determines whether pressing secondary with a value proxy in hand will create a ProtoFlux input with that value.", () => true);
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> KEY_CLICK_VALUES = new("click_values", "Determines whether pressing primary while hovering over a text field while holding a value proxy will put the held value into that field.", () => true);
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> KEY_INSPECTOR_BUTTONS = new("inspector_buttons", "Determines whether inspector panels should generate the pick value button", () => true);
        // future update ? 
        //[AutoRegisterConfigKey]
        //public static ModConfigurationKey<bool> KEY_VALUE_EXTRAS = new("value_extras", "Determines whether value proxies should generate extra visuals sometimes (showing the color of colors).", () => true);

        public static ModConfiguration config;

        public override void OnEngineInit()
        {
            config = GetConfiguration();
            Harmony harmony = new Harmony("me.art0007i.ValueProxyExtensions");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(SyncMemberEditorBuilder))]
        class InspectorFieldPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch("BuildMemberEditors")]
            public static void InspectorFieldBuilder(IField field, Type type, UIBuilder ui, string path)
            {
                if (!config.GetValue(KEY_INSPECTOR_BUTTONS)) return;
                if (type.IsPrimitive ||
                    type == typeof(string) ||
                    type == typeof(Uri) ||
                    type == typeof(Type) ||
                    type == typeof(decimal)
                )
                {
                    return;
                }
                if (type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length > 0)
                {
                    if(type == typeof(colorX))
                    {
                        ui.NestOut();
                    }
                    GrabButtonMethod.MakeGenericMethod(type).Invoke(null, new object[] { field, type, ui, path });
                }
            }

            public static MethodInfo GrabButtonMethod = AccessTools.Method(typeof(InspectorFieldPatch), nameof(BuildGrabButton));

            public static void BuildGrabButton<T>(IField field, Type type, UIBuilder ui, string path = null)
            {
                // ignore inner fields, for example Rect.position and Rect.size
                if (!string.IsNullOrEmpty(path)) return;

                ui.PushStyle();
                ui.Style.MinWidth = 24;
                ui.Style.ButtonColor = new colorX(0.7f, 0.7f, 1);
                var bt = ui.Button(Constants.grabIcon);
                bt.ColorDrivers.Do((cd) => cd.PressColor.Value = cd.HighlightColor.Value);

                bt.Slot.AttachComponent<ValueProxySource<T>>().Value.DriveFrom(field);

                // this could throw sometimes maybe?
                bt.Slot.AttachComponent<ValueReceiver<T>>().Field.Target = (IField<T>)field;

                ui.PopStyle();
            }
        }

        [HarmonyPatch(typeof(TextField), "FrooxEngine.IButtonPressReceiver.Pressed")]
        class TextFieldPatch
        {
            public static bool Prefix(ButtonEventData eventData, TextField __instance)
            {
                if (!config.GetValue(KEY_CLICK_VALUES)) return true;
                var grabber = eventData.source.Slot.TryFindGrabberWithItems();
                if (grabber != null && grabber.GetValueProxy() != null)
                {
                    return !__instance.TryReceive(grabber.GrabbedObjects, grabber, null, grabber.Slot.GlobalPosition);
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(UserHelper), "TryFindGrabberWithItems")]
        class GrabberChiralityFix
        {
            public static bool Prefix(Slot source, ref Grabber __result)
            {
                // Receive Order: same hand > same hand userspace > opposite hand > opposite hand userspace

                var root = source.ActiveUserRoot;
                if (root == null) return true;

                // Find grabber corresponding to the hand the user clicked with
                Grabber grabber = root.Slot.GetComponentInChildren<Grabber>((gr) => gr.CorrespondingBodyNode.Value != BodyNode.NONE && source.IsChildOf(gr.Slot.Parent.Parent));
                if (grabber == null) return true;

                Chirality side = grabber.CorrespondingBodyNode.Value.GetChirality();
                Chirality otherSide = side.GetOther();

                if (side < 0) return true;

                if (!grabber.HasProxy()) grabber = null;

                World other = source.World.IsUserspace() ? source.Engine.WorldManager.FocusedWorld : Userspace.UserspaceWorld;
                var otherRoot = other.LocalUser?.Root.Slot;

                // If we haven't found a grabber with proxies yet, try finding same hand grabber in userspace
                if (grabber == null && config.GetValue(KEY_PROXY_TRANSFER)) grabber = otherRoot.FindSidedGrabberWithProxy(side);
                // If we haven't found a grabber with proxies yet, try finding opposite hand grabber
                if (grabber == null) grabber = root.Slot.FindSidedGrabberWithProxy(otherSide);
                // If we haven't found a grabber with proxies yet, try finding same hand grabber in userspace
                if (grabber == null && config.GetValue(KEY_PROXY_TRANSFER)) grabber = otherRoot.FindSidedGrabberWithProxy(otherSide);
                

                if (grabber != null)
                {
                    __result = grabber;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ProtoFluxTool), "OnSecondaryPress")]
        class FluxToolPatch
        {
            public static bool Prefix(ProtoFluxTool __instance)
            {
                if (!config.GetValue(KEY_CREATE_INPUTS)) return true;

                var grabber = __instance.ActiveHandler.Grabber;
                IValueSource valProx = null;
                ReferenceProxy refProx = grabber.GetReferenceProxy(); ;
                Type type = null;
                if (refProx == null)
                {
                    valProx = grabber.GetValueProxy();
                    type = valProx?.GetType().GenericTypeArguments.FirstOrDefault();
                }
                else
                {
                    type = refProx?.Reference.Target?.GetType();
                }
                if (type != null)
                {
                    Type defaultInput = ProtoFluxHelper.GetInputNode(type);
                    if (defaultInput == null)
                    {
                        defaultInput = typeof(ValueObjectInput<string>);
                        type = typeof(string);
                    }
                    var node = __instance.SpawnNode(defaultInput, (node) =>
                    {
                        if (refProx != null)
                        {
                            Traverse.Create(node).Field("Target")?.Property("Target")?.SetValue(refProx.Reference.Target);
                        }
                        if (valProx != null)
                        {
                            Traverse.Create(node).Field("Value")?.Property("Value")?.SetValue(valProx.BoxedValue);
                        }
                    });
                    return false;
                }

                return true;
            }
        }


        [HarmonyPatch(typeof(ReferenceProxy), "Construct")]
        class ReferenceProxyExtensiomsPatch
        {
            public static void Postfix(IGrabbable __result, IWorldElement target)
            {
                if (config.GetValue(KEY_REF_VISUAL))
                {
                    __result.Slot.GetComponentInChildren<Canvas>().Size.Value = new(0, 32);
                    __result.Slot.GetComponentInChildren<Text>().Content.Value += "\n" + target.GetType().GetNiceName();
                }

                if (config.GetValue(KEY_REF_VALUE_PROXY))
                {
                    __result.Slot.AttachComponent<ValueProxy<string>>().Value.Value = target.GetType().FullName;
                }
            }
        }
    }
}