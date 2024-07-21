using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAVis.API;

internal enum ElementColor : byte
{
    White,
    Red,
    Green,
}

internal sealed class ElementArray : IEnumerable
{
    private (uint val, ElementColor col)[] _elements;
    public int Size { get; private set; }
    public uint? this[uint index]
    {
        get
        {
            if(index < Size)
            {
                _elements[(int)index].col = ElementColor.Red;
                return _elements[(int)index].val;
            }
            return null;
        }
        set
        {
            if (index < Size && value is uint v)
                _elements[(int)index].val = v;
        }
    }
    internal ElementArray(int size)
    {
        _elements = new (uint, ElementColor)[size];
        Size = size;

        for(uint i = 0; i < Size; i++)
        {
            _elements[(int)i] = (i + 1, ElementColor.White);
        }
    }

    internal void Shuffle()
    {
        Random.Shared.Shuffle(_elements);
    }

    internal void WhiteAll()
    {
        for(uint i = 0; i < Size; i++)
        {
            _elements[i].col = ElementColor.White;
        }
    }
    internal bool Swap(uint indexA, uint indexB)
    {
        var a = this[indexA];
        var b = this[indexB];
        if (a is not null && b is not null)
        {
            _elements[indexA] = (b.Value, ElementColor.Green);
            _elements[indexB] = (a.Value, ElementColor.Green);
            return true;
        }
        return false;
    }
    public IEnumerator GetEnumerator()
    {
        return _elements.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
