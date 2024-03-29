﻿using Elements.Core;
using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValueProxyExtensions
{
    internal static class GrabberExtensions
    {
        public static Grabber FindSidedGrabberWithProxy(this Slot slot, Chirality side)
        {
            return slot.GetComponentInChildren<Grabber>((gr) => gr.CorrespondingBodyNode.Value.GetChirality() == side && gr.HasProxy());
        }
        public static bool HasProxy(this Grabber grabber)
        {
            return grabber.GetValueProxy() != null || grabber.GetReferenceProxy() != null;
        }
        public static IValueSource GetValueProxy(this Grabber grabber)
        {
            return grabber.GrabbedObjects.Select((gr) => gr.Slot.GetComponent<IValueSource>()).FirstOrDefault((p) => p != null);
        }
        public static ValueProxy<T> GetValueProxy<T>(this Grabber grabber)
        {
            return grabber.GrabbedObjects.Select((gr) => gr.Slot.GetComponent<ValueProxy<T>>()).FirstOrDefault((p) => p != null);
        }
        public static ReferenceProxy GetReferenceProxy(this Grabber grabber)
        {
            return grabber.GrabbedObjects.Select((gr) => gr.Slot.GetComponent<ReferenceProxy>()).FirstOrDefault((p) => p != null);
        }
    }
}
