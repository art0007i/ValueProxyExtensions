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
        public static ModConfigurationKey<bool> KEY_CREATE_INPUTS = new("create_inputs", "Determines whether pressing secondary with a value proxy in hand will create a logix input with that value.", () => true);
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> KEY_CLICK_VALUES = new("click_values", "Determines whether pressing primary while hovering over a text field while holding a value proxy will put the held value into that field.", () => true);
        // future update ? 
        //[AutoRegisterConfigKey]
        //public static ModConfigurationKey<bool> KEY_VALUE_EXTRAS = new("value_extras", "Determines whether value proxies should generate extra visuals sometimes (showing the color of colors).", () => true);
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> KEY_LOGIX_DISPLAYS = new("logix_displays", "Determines whether logix displays should generate the pick value button", () => true);
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> KEY_INSPECTOR_BUTTONS = new("inspector_buttons", "Determines whether inspector panels should generate the pick value button", () => true);

        public static ModConfiguration config;
        private static Dictionary<Type, List<FieldInfo>> displayTexts = new();

        public override void OnEngineInit()
        {
            config = GetConfiguration();
            Harmony harmony = new Harmony("me.art0007i.ValueProxyExtensions");
            harmony.PatchAll();

            /*foreach (var display in Constants.displays)
            {
                var visual = AccessTools.Method(display, "OnGenerateVisual");
                var changes = AccessTools.Method(display, "OnChanges");
                if (visual != null)
                {
                    displayTexts[display] = new();
                    var test = AccessTools.Field(display, "_text");
                    if (test != null)
                    {
                        displayTexts[display].Add(test);
                    }
                    for (var i = 0; i < 4; i++)
                    {
                        var newf = AccessTools.Field(display, "_text" + i);
                        if (newf != null)
                        {
                            displayTexts[display].Add(newf);
                        }
                    }
                    harmony.Patch(visual, postfix: new HarmonyMethod(AccessTools.Method(typeof(DisplayPatch), "Postfix")));
                    harmony.Patch(changes, postfix: new HarmonyMethod(AccessTools.Method(typeof(DisplayPatch), "ChangesPostfix")));
                }
            }*/
        }

        
        class DisplayPatch
        {/*
            public static void Postfix(Slot root, ProtoFluxNode __instance)
            {
                if (!config.GetValue(KEY_LOGIX_DISPLAYS)) return;
                Slot bt = null;
                var ley = root[0][0].Find("Vertical Layout");
                MultiValueTextFormatDriver formatter = null;
                if (__instance is FrooxEngine.ProtoFlux.Display.Display_Color)
                {
                    bt = ley[0].Find("Image");
                    var cd = (AccessTools.Field(__instance.GetType(), "_color").GetValue(__instance) as FieldDrive<color>);
                    var antiError = cd.Target;
                    cd.Target = null;
                    // If the button sees that an image exists on the same slot it will try to drive it, but fail because it's already driven by something else
                    // To prevent this I undrive the image, create the button (which automatically drives the image), undrive the image, then drive it from the original source.
                    bt.AttachComponent<Button>().ColorDrivers.Clear();
                    cd.ForceLink(antiError);
                    formatter = bt.AttachComponent<MultiValueTextFormatDriver>();
                    formatter.Sources.Add().Target = antiError;
                    formatter.Format.Value = "{0}";
                }
                else
                {
                    var foundDrives = displayTexts[__instance.GetType()].Select((field) => (field.GetValue(__instance) as FieldDrive<string>).Target); ;
                    var txtCount = displayTexts[__instance.GetType()].Count;



                    var ui = new UIBuilder(root[0][0]);
                    ley.Parent = ui.HorizontalLayout(0, 4, Alignment.MiddleCenter).Slot;
                    var le = ley.AttachComponent<LayoutElement>();
                    le.MinWidth.Value = 1f; le.FlexibleWidth.Value = 1f;
                    ui.Style.Width = 16;
                    var button = ui.Button(Constants.grabIcon, MathX.Lerp(color.Blue, color.White, 0.5f));
                    var cd = button.ColorDrivers.Single(); cd.PressColor.Value = cd.HighlightColor.Value;
                    bt = button.Slot;
                    bt.OrderOffset = 10;

                    if (txtCount > 1)
                    {
                        bt.PersistentSelf = false;
                        bt.AttachComponent<DestroyOnUserLeave>().TargetUser.Target = bt.LocalUser;
                    }
                    else
                    {
                        // it would be so awesome if this worked...
                        // Problem is vector types such as float2 will create an output like [X: 1; Y: 2]
                        //                                        but the required format is [1; 2]
                        
                        var fmt = "";
                        var i = 0;
                        foundDrives.Do((field) => fmt += $"{{{i++}}}; ");
                        if (txtCount == 0) return;
                        fmt = fmt.Remove(fmt.Length - 2);
                        if (txtCount > 1)
                            fmt = "[" + fmt + "]";
                        formatter = bt.AttachComponent<MultiValueTextFormatDriver>();
                        formatter.Format.Value = "{0}";
                        formatter.Sources.AddRange(foundDrives);
                    }
                }
                        
                        
                var text = bt.AttachComponent<Text>();
                var textField = bt.AttachComponent<TextField>();
                bt.AttachComponent<TypeField>().Type.Value = AccessTools.Field(__instance.GetType(), "Source").FieldType.GetGenericArguments()[0];

                if (formatter != null)
                {
                    formatter.Text.Target = text.Content;
                }
                textField.Editor.Target.Text.Target = text;
                textField.Enabled = false;
                text.Enabled = false;
            }
            public static void ChangesPostfix(ProtoFluxNode __instance)
            {
                if (!__instance.HasActiveVisual()) return;
                if (displayTexts[__instance.GetType()].Count <= 1) return;

                Sync<string> txt = null;
                try
                {
                    txt = __instance.Slot[0]?[0]?[0]?.FindChild("Horizontal Layout")?[1]?.GetComponent<Text>()?.Content;
                }
                catch
                {
                    return;
                }
                if (txt == null) return;
                User allocUser = null;
                ulong num;
                byte b;
                txt.ReferenceID.ExtractIDs(out num, out b);
                allocUser = txt.World.GetUserByAllocationID(b);
                if (allocUser == null) return;
                if (num < allocUser.AllocationIDStart)
                {
                    allocUser = null;
                }
                if (allocUser != __instance.LocalUser) return;

                var funnyStrs = displayTexts[__instance.GetType()].ConvertAll((fi) => (fi.GetValue(__instance) as FieldDrive<string>).Target?.Value.Substring(3));
                txt.Value = "[" + string.Join("; ", funnyStrs) + "]";
            }
            */
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
                    type == typeof(colorX) ||
                    type == typeof(BoundingBox) || 
                    type == typeof(Rect) || // This one could be parseable but im too lazy to add it lol
                    type.IsNullable()
                )
                {
                    return;
                }
                if (type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length > 0)
                {
                    // Could maybe disable value picker button if nullable is false but 2 lazy for that lol
                    BuildGrabButton(field, ui, path);
                }
            }

            public static void BuildGrabButton(IField field, UIBuilder ui, string path = null)
            {
                ui.PushStyle();
                ui.Style.MinWidth = 24;
                ui.Style.ButtonColor = new colorX(0.7f, 0.7f, 1);
                var bt = ui.Button(Constants.grabIcon);
                bt.ColorDrivers.Do((cd)=> cd.PressColor.Value = cd.HighlightColor.Value);
                var text = bt.Slot.AttachComponent<Text>();
                var textField = bt.Slot.AttachComponent<TextField>();
                var primEditor = bt.Slot.AttachComponent<PrimitiveMemberEditor>();
                (AccessTools.Field(primEditor.GetType(), "_textEditor").GetValue(primEditor) as SyncRef<TextEditor>).Target = (textField.Editor);
                (AccessTools.Field(primEditor.GetType(), "_target").GetValue(primEditor) as RelayRef<IField>).Target = field;
                (AccessTools.Field(primEditor.GetType(), "_textDrive").GetValue(primEditor) as FieldDrive<string>).Target = text.Content;
                (AccessTools.Field(primEditor.GetType(), "_path").GetValue(primEditor) as Sync<string>).Value = path;

                textField.Editor.Target.Text.Target = text;
                textField.Enabled = false;
                text.Enabled = false;
                ui.PopStyle();
            }
        }

        [HarmonyPatch(typeof(TextField))]
        class ValueProxyExtensionsPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch("TryGrab")]
            public static void Postfix(IGrabbable __result, TextField __instance)
            {
                if (__result != null)
                {
                    if (__instance.Text.Content.IsDriven)
                    {
                        var editor = (__instance.Text.Content.ActiveLink as SyncElement)?.Component;
                        if (editor is MemberEditor)
                        {
                            var com = editor as MemberEditor;
                            var myfield = __result.Slot.AttachComponent<TypeField>();
                            myfield.Type.Value = com.Accessor.TargetType;
                            return;
                        }
                    }
                    if (__instance.Editor?.Target.EditingFinished.Target != null)
                    {
                        var targetEl = (AccessTools.Method(typeof(ISyncRef), "get_Target").Invoke(__instance.Editor.Target.EditingFinished, null) as IWorldElement);
                        if (typeof(ProtoFluxNode).IsAssignableFrom(targetEl.GetType()))
                        {
                            var myfield = __result.Slot.AttachComponent<TypeField>();
                            // types such as int4, will generate the type int4 when you pull only one number, difficult to fix
                            // this could be error prone if you pull a text out of any logix node that isn't generic but has a text field
                            myfield.Type.Value = targetEl.GetType().BaseType.GetGenericArguments()[0];
                            return;
                        }
                    }
                    var typeField = __instance.Slot.GetComponent<TypeField>();
                    if (typeField != null)
                    {
                        var myfield = __result.Slot.AttachComponent<TypeField>();
                        myfield.Type.Value = typeField.Type.Value;
                        return;
                    }
                }
            }
        }
        [HarmonyPatch(typeof(TextField), "FrooxEngine.IButtonPressReceiver.Pressed")]
        class TextFieldPatch
        {
            public static bool Prefix(ButtonEventData eventData, TextField __instance)
            {
                if (!config.GetValue(KEY_CLICK_VALUES)) return true;
                var grabber = eventData.source.Slot.TryFindGrabberWithItems();
                if (grabber != null && grabber.GetValueProxy<string>() != null)
                {
                    __instance.TryReceive(grabber.GrabbedObjects, grabber, null, grabber.Slot.GlobalPosition);
                    return false;
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
                    else if(grabber.GetValueProxy<string>() == null && grabber.GetReferenceProxy() == null) grabber = root.Slot.GetComponentInChildren<Grabber>((gr) => gr.CorrespondingBodyNode.Value.GetChirality() == side.GetOther());
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
        class LogixTipPatch
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
        }
        */
        [HarmonyPatch(typeof(PrimitiveTryParsers), "GetParser", new Type[] { typeof(Type) })]
        class ParserExtensions
        {
            public static Exception Finalizer(Exception __exception, ref PrimitiveTryParsers.TryParser __result, Type type)
            {
                if(__exception != null)
                {
                    MethodInfo pars;
                    if (type.IsEnum) pars = AccessTools.FirstMethod(typeof(Enum), (mi) => mi.Name == "TryParse" && mi.GetParameters().Length == 2).MakeGenericMethod(type);
                    else pars = AccessTools.FirstMethod(typeof(RobustParser), (mi) => mi.GetParameters().Last().ParameterType == type.MakeByRefType());
                    
                    if (pars == null)
                    {
                        return __exception;
                    }
                    __result = delegate (string str, out object obj)
                    {
                        object[] parsed = new object[] { str, null };
                        if ((bool)pars.Invoke(null, parsed))
                        {
                            obj = parsed[1];
                            return true;
                        }
                        obj = null; 
                        return false;
                    };
                }
                return null;
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
                    //__result.Slot.AttachComponent<TypeField>().Type.Value = typeof(Type);
                }
            }
        }
    }
}