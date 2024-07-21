using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAVis.API;

public sealed class SortingContext
{
    readonly ElementArray _elementArray;

    public int ArraySize => _elementArray.Size;

    internal SortingContext(ElementArray elementArray)
    {
        _elementArray = elementArray;
    }

    public uint? GetValueAt(uint index)
    {
        return _elementArray[index];
    }
    public bool SwapValues(uint indexA, uint indexB) 
    {
        return _elementArray.Swap(indexA, indexB);
    }
}
