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
        public override string Version => "2.0.0";
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
        // future update ? 
        //[AutoRegisterConfigKey]
        //public static ModConfigurationKey<bool> KEY_VALUE_EXTRAS = new("value_extras", "Determines whether value proxies should generate extra visuals sometimes (showing the color of colors).", () => true);
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> KEY_INSPECTOR_BUTTONS = new("inspector_buttons", "Determines whether inspector panels should generate the pick value button", () => true);

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
                    type == typeof(decimal) ||
                    type == typeof(color) ||
                    type == typeof(colorX)
                )
                {
                    return;
                }
                if (type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length > 0)
                {
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
                if (!config.GetValue(KEY_PROXY_TRANSFER)) return true;

                var root = source.ActiveUserRoot;
                Grabber grabber = root.Slot.GetComponentInChildren<Grabber>((gr) => gr.CorrespondingBodyNode.Value != BodyNode.NONE && source.IsChildOf(gr.Slot.Parent.Parent));
                if (grabber == null) return true;
                Chirality side = grabber.CorrespondingBodyNode.Value.GetChirality();
                World other = source.World.IsUserspace() ? source.Engine.WorldManager.FocusedWorld : Userspace.UserspaceWorld;
                var otherRoot = other.LocalUser.Root;
                if (side >= 0)
                {
                    var newGrabber = otherRoot.Slot.GetComponentInChildren<Grabber>((gr) => gr.CorrespondingBodyNode.Value.GetChirality() == side);
                    if (newGrabber.GetValueProxy<string>() != null || newGrabber.GetReferenceProxy() != null) grabber = newGrabber;
                    else if (grabber.GetValueProxy<string>() == null && grabber.GetReferenceProxy() == null) grabber = root.Slot.GetComponentInChildren<Grabber>((gr) => gr.CorrespondingBodyNode.Value.GetChirality() == side.GetOther());
                }
                if (grabber != null)
                {
                    __result = grabber;
                    return false;
                }
                Debug("Using fallback original grabber detection.");
                return true;
            }
        }
        
        /*
        [HarmonyPatch(typeof(ProtoFluxTool), "OnSecondaryPress")]
        class FluxToolPatch
        {
            public static bool Prefix(ProtoFluxTool __instance)
            {
                if (!config.GetValue(KEY_CREATE_INPUTS)) return true;

                var grabber = __instance.ActiveHandler.Grabber;
                var prox = grabber.GetValueProxy<string>();
                if (prox != null && grabber.GetReferenceProxy() == null)
                {
                    var typeField = prox.Slot.GetComponent<TypeField>();
                    Type type = typeField?.Type.Value ?? typeof(string);
                    if (type != null)
                    {
                        Type defaultInput = null;
                        try
                        {
                            defaultInput = typeof(ValueInput<>).MakeGenericType(type);
                        }

                        if (defaultInput == null)
                        {
                            defaultInput = typeof(ValueInput<string>);
                            type = typeof(string);
                        }
                        var node = ((ProtoFluxNode)((Slot)AccessTools.Method(typeof(ProtoFluxTool), "CreateNewNodeSlot").Invoke(__instance, new object[] { (defaultInput).GetNiceName() })).AttachComponent(defaultInput));
                        if (node is TypeObjectInput)
                        {
                            var t = (AccessTools.Field(typeof(TypeObjectInput), "_value").GetValue(node) as SyncType);
                            t.Value = TypeHelper.FindType(prox.Value.Value);
                            node.GenerateVisual();
                            return false;
                        }
                        try
                        {
                            var nodeSyncField = node.GetSyncMember(4);
                            if (type.IsEnum)
                            {
                                var en = Enum.Parse(type, prox.Value.Value);

                                AccessTools.Method(nodeSyncField.GetType(), "set_Value").Invoke(nodeSyncField, new object[] { en });
                            }
                            else
                            {
                                var pars = AccessTools.FirstMethod(typeof(RobustParser), (mi) => mi.GetParameters().Last().ParameterType == type.MakeByRefType());
                                object[] parsed = new object[] { prox.Value.Value, null };
                                if ((bool)pars.Invoke(null, parsed))
                                {
                                    AccessTools.Method(nodeSyncField.GetType(), "set_Value").Invoke(nodeSyncField, new object[] { parsed[1] });
                                }
                                else
                                {
                                    // this should only really happen when you do weird stuff
                                    // such as manually editing / constructing a value proxy
                                    Debug("epic parser failure!!!! laugh at this user");
                                }
                            }
                        }
                        catch
                        {
                            Warn("an exception occured while trying to parse " + type);
                        }
                        finally
                        {
                            node.GenerateVisual();
                        }
                        return false;
                    }
                }
                return true;
            }
        }*/
        

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