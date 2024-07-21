using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raylib_CsLo;
namespace SAVis;

internal static class Helpers
{
    public static int GuiGetStyle(GuiControl control, GuiControlProperty property) =>
        RayGui.GuiGetStyle((int)control, (int)property);
    public static int GuiGetStyle(GuiControl control, GuiTextBoxProperty textBoxProperty) =>
        RayGui.GuiGetStyle((int)control, (int)textBoxProperty);
    public static int GuiGetStyle(GuiControl control, GuiDefaultProperty defaultProperty) =>
        RayGui.GuiGetStyle((int)control, (int)defaultProperty);
    public static void GuiSetStyle(GuiControl control, GuiControlProperty property, int value) =>
        RayGui.GuiSetStyle((int)control, (int)property, value);
}

internal static class Extensions
{
    public static IEnumerable<string> PadRightString(this IEnumerable<string> src, int len)
    {
        int i = 0;
        foreach (var t in src)
        {
            yield return t;
            i++;
        }
        if ((len -= i) <= 0)
            yield break;
        for (; len > 0; len--)
        {
            yield return "";
        }
    }
}
