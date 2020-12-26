using System;
using System.Collections.Generic;
using System.Text;

namespace Il2CppInspector.PluginAPI
{
    /// <summary>
    /// Setting this attribute on an interface method will allow the method to be called again
    /// while it is already running, ie. it enables recursion. Disabled by default.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ReentrantAttribute : Attribute { }
}
