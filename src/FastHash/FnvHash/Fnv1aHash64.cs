﻿//Ported to C# by Ian Qvist
//Source: http://www.isthe.com/chongo/src/fnv/hash_64a.c

using System.Runtime.CompilerServices;
using static Genbox.FastHash.FnvHash.FnvHashConstants;

namespace Genbox.FastHash.FnvHash;

/// <summary>
/// Fowler–Noll–Vo hash implementation
/// </summary>
public static class Fnv1aHash64
{
    public static ulong ComputeHash(byte[] data)
    {
        ulong hash = FnvInit64;

        for (int i = 0; i < data.Length; i++)
        {
            hash ^= data[i];
            hash *= FnvPrime64;
        }

        return hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ComputeIndex(ulong input)
    {
        ulong hash = FnvInit64;
        hash ^= input;
        hash *= FnvPrime64;
        return hash;
    }
}