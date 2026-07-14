using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.NET.Common;
using BepInExResoniteShim;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes;
using FrooxEngine.UIX;
using HarmonyLib;
using Microsoft.VisualBasic;
using Renderite.Shared;

namespace ValueProxyExtensions;

[ResonitePlugin(PluginMetadata.GUID, PluginMetadata.NAME, PluginMetadata.VERSION, PluginMetadata.AUTHORS,
    PluginMetadata.REPOSITORY_URL)]
[BepInDependency(BepInExResoniteShim.PluginMetadata.GUID, BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BasePlugin
{
    public readonly static Uri grabIcon =
        new Uri("resdb:///702b51521a39f6a0e5d5d36b2675638e90d395695134678e04151c9a78cbfd6f");

#nullable disable
    internal static new ManualLogSource Log;
    internal static ConfigEntry<bool> ReferenceProxyVisual;
    internal static ConfigEntry<bool> ReferenceProxyTypeName;
    internal static ConfigEntry<bool> ProxyTransfer;
    internal static ConfigEntry<bool> CreateInputs;
    internal static ConfigEntry<bool> ClickValues;
    internal static ConfigEntry<bool> InspectorButtons;
#nullable enable

    public override void Load()
    {
        Log = base.Log;

        ReferenceProxyVisual = Config.Bind("General", "ReferenceProxyVisual", true,
            "Determines whether text containing reference type should be generated on reference proxies.");
        ReferenceProxyTypeName = Config.Bind("General", "ReferenceProxyTypeName", false,
            "Determines whether reference proxies should act as value proxies with the full type name.");
        ProxyTransfer = Config.Bind("General", "ProxyTransfer", true,
            "Determines whether value proxies should be allowed to be clicked in to userspace and back.");
        CreateInputs = Config.Bind("General", "CreateInputs", true,
            "Determines whether pressing secondary with a value or reference proxy in hand will create a ProtoFlux input with that value.");
        ClickValues = Config.Bind("General", "ClickValues", true,
            "Determines whether pressing primary while hovering over a text field while holding a value proxy will put the held value into that field.");
        InspectorButtons = Config.Bind("General", "InspectorButtons", false,
            "Determines whether inspector panels should generate the pick value button.");


        HarmonyInstance.PatchAll();
    }

    [HarmonyPatch(typeof(SyncMemberEditorBuilder))]
    class InspectorFieldPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("BuildMemberEditors")]
        public static void InspectorFieldBuilder(IField field, Type type, UIBuilder ui, string path)
        {
            if (!InspectorButtons.Value) return;
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
                GrabButtonMethod.MakeGenericMethod(type).Invoke(null, new object[] { field, type, ui, path });
            }
        }

        public static MethodInfo GrabButtonMethod =
            AccessTools.Method(typeof(InspectorFieldPatch), nameof(BuildGrabButton));

        public static void BuildGrabButton<T>(IField field, Type type, UIBuilder ui, string path = null)
        {
            // ignore inner fields, for example Rect.position and Rect.size
            if (!string.IsNullOrEmpty(path)) return;

            ui.PushStyle();
            ui.Style.MinWidth = 24;
            ui.Style.ButtonColor = new colorX(0.7f, 0.7f, 1);
            var bt = ui.Button(grabIcon);
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
            if (!ClickValues.Value) return true;
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
            Grabber? grabber = root.Slot.GetComponentInChildren<Grabber>((gr) =>
                gr.CorrespondingBodyNode.Value != BodyNode.NONE && source.IsChildOf(gr.Slot.Parent.Parent));
            if (grabber == null) return true;

            Chirality side = grabber.CorrespondingBodyNode.Value.GetChirality();
            Chirality otherSide = side.GetOther();

            if (side < 0) return true;

            if (!grabber.HasProxy()) grabber = null;

            World other = source.World.IsUserspace()
                ? source.Engine.WorldManager.FocusedWorld
                : Userspace.UserspaceWorld;
            var otherRoot = other.LocalUser?.Root.Slot;

            // If we haven't found a grabber with proxies yet, try finding same hand grabber in userspace
            if (grabber == null && otherRoot != null && ProxyTransfer.Value) grabber = otherRoot.FindSidedGrabberWithProxy(side);
            // If we haven't found a grabber with proxies yet, try finding opposite hand grabber
            if (grabber == null) grabber = root.Slot.FindSidedGrabberWithProxy(otherSide);
            // If we haven't found a grabber with proxies yet, try finding opposite hand grabber in userspace
            if (grabber == null && otherRoot != null && ProxyTransfer.Value) grabber = otherRoot.FindSidedGrabberWithProxy(otherSide);


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
            if (!CreateInputs.Value) return true;

            var grabber = __instance.ActiveHandler?.Grabber;

            if (grabber == null) return true;

            IValueSource? valProx = null;
            ReferenceProxy? refProx = grabber.GetReferenceProxy();
            
            Type? type = null;
            if (refProx == null)
            {
                valProx = grabber.GetValueProxy();
                type = valProx?.GetType().GenericTypeArguments.FirstOrDefault();
            }
            else
            {
                type = refProx.Reference.Target?.GetType();
            }

            if (type != null)
            {
                Type defaultInput = ProtoFluxHelper.GetInputNode(type);
                if (defaultInput == null)
                {
                    defaultInput = typeof(ValueObjectInput<string>);
                    type = typeof(string);
                }

                var node = __instance.SpawnNode(defaultInput, node =>
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
            if (ReferenceProxyVisual.Value)
            {
                __result.Slot.GetComponentInChildren<Canvas>().Size.Value = new(0, 32);
                __result.Slot.GetComponentInChildren<Text>().Content.Value += "\n" + target.GetType().GetNiceName();
            }

            if (ReferenceProxyTypeName.Value)
            {
                __result.Slot.AttachComponent<ValueProxy<string>>().Value.Value = target.GetType().FullName;
            }
        }
    }
}