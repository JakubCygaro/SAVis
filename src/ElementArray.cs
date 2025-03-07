﻿using System;
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
    private (uint val, ElementColor col)[] m_elements;
    public int Size { get; private set; }
    public uint? this[uint index]
    {
        get
        {
            if(index < Size)
            {
                m_elements[(int)index].col = ElementColor.Red;
                return m_elements[(int)index].val;
            }
            return null;
        }
        set
        {
            if (index < Size && value is uint v)
                m_elements[(int)index].val = v;
        }
    }
    internal ElementArray(int size)
    {
        m_elements = new (uint, ElementColor)[size];
        Size = size;

        for(uint i = 0; i < Size; i++)
        {
            m_elements[(int)i] = (i + 1, ElementColor.White);
        }
    }

    internal void Shuffle()
    {
        Random.Shared.Shuffle(m_elements);
    }

    internal void WhiteAll()
    {
        for(uint i = 0; i < Size; i++)
        {
            m_elements[i].col = ElementColor.White;
        }
    }
    internal bool Swap(uint indexA, uint indexB)
    {
        var a = this[indexA];
        var b = this[indexB];
        if (a is not null && b is not null)
        {
            m_elements[indexA] = (b.Value, ElementColor.Green);
            m_elements[indexB] = (a.Value, ElementColor.Green);
            return true;
        }
        return false;
    }

    internal void Sort()
    {
        Array.Sort(m_elements);
    }
    public IEnumerator GetEnumerator()
    {
        return m_elements.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
