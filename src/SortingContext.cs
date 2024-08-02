using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAVis.API;

public sealed class SortingContext
{
    ElementArray m_elementArray;

    public int ArraySize => m_elementArray.Size;

    internal SortingContext(ElementArray elementArray)
    {
        m_elementArray = elementArray;
    }
    internal void TakeNewArray(ElementArray newElementArray)
    {
        m_elementArray = newElementArray;
    }
    public uint? GetValueAt(uint index)
    {
        return m_elementArray[index];
    }
    public bool SwapValues(uint indexA, uint indexB) 
    {
        return m_elementArray.Swap(indexA, indexB);
    }
}
