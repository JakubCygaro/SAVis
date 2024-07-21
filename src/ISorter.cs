using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAVis.API;

public interface ISorter
{
    string Name { get; }
    string Author { get; }

    /// <summary>
    /// Called before every draw
    /// </summary>
    /// <returns></returns>
    IEnumerator<bool> Update(SortingContext ctx);

    internal static ISorter Default => new DefaultSorter();
}

file class DefaultSorter : ISorter
{
    public string Name => "Default sorter";
    public string Author => "Adam Papieros";

    uint _current = 0;
    public IEnumerator<bool> Update(SortingContext ctx)
    {
        bool unsorted = true;
        while (unsorted)
        {
            unsorted = false;
            for (uint i = 0; i < ctx.ArraySize - 1; i++) 
            {
                var a = ctx.GetValueAt(i);
                var b = ctx.GetValueAt(i + 1);
                if (a > b)
                {
                    ctx.SwapValues(i, i + 1);
                    unsorted = true;
                }
                yield return false;
            }
        }
        yield return true;
    }
}
