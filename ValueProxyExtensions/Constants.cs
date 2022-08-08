using FrooxEngine.LogiX.Display;
using System;

namespace ValueProxyExtensions
{
    class Constants
    {
        // List obtained using reflection, hard coded to not iterate over every single type each time the game starts.
        /*
        Code used to generate this list

            var sb = new StringBuilder();
            foreach (var type in typeof(FrooxEngine.Engine).Assembly.GetTypes()) 
            {
                type.GetCustomAttributes(true).FirstOrDefault((o) =>
                {
                    if(o.GetType() == typeof(NodeOverload))
                    {
                        return ((NodeOverload)o).FunctionName == "Display";
                    }
                    return false;
                });
            }
            UniLog.Log(sb.ToString());
         */
        public static readonly Type[] displays = {
            //typeof(Display_Dummy), Does not actually generate a unique visual
            typeof(Display_Object),
            typeof(Display_Uri),
            typeof(Display_Bool),
            typeof(Display_Byte),
            typeof(Display_Ushort),
            typeof(Display_Uint),
            typeof(Display_Ulong),
            typeof(Display_Sbyte),
            typeof(Display_Short),
            typeof(Display_Int),
            typeof(Display_Long),
            typeof(Display_Float),
            typeof(Display_Double),
            typeof(Display_Decimal),
            typeof(Display_Char),
            typeof(Display_String),
            typeof(Display_Bool2),
            typeof(Display_Uint2),
            typeof(Display_Ulong2),
            typeof(Display_Int2),
            typeof(Display_Long2),
            typeof(Display_Float2),
            typeof(Display_Double2),
            typeof(Display_Bool3),
            typeof(Display_Uint3),
            typeof(Display_Ulong3),
            typeof(Display_Int3),
            typeof(Display_Long3),
            typeof(Display_Float3),
            typeof(Display_Double3),
            typeof(Display_Bool4),
            typeof(Display_Uint4),
            typeof(Display_Ulong4),
            typeof(Display_Int4),
            typeof(Display_Long4),
            typeof(Display_Float4),
            typeof(Display_Double4),
            typeof(Display_FloatQ),
            typeof(Display_DoubleQ),
            typeof(Display_DateTime),
            typeof(Display_TimeSpan),
            typeof(Display_Color),
            typeof(Display_ColorX)
        };
        public readonly static Uri grabIcon = new Uri("neosdb:///702b51521a39f6a0e5d5d36b2675638e90d395695134678e04151c9a78cbfd6f");
    }
}
