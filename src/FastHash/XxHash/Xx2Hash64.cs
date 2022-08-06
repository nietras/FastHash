﻿/*
*  xxHash - Fast Hash algorithm
*  Copyright (C) 2012-2016, Yann Collet
*
*  BSD 2-Clause License (http://www.opensource.org/licenses/bsd-license.php)
*
*  Redistribution and use in source and binary forms, with or without
*  modification, are permitted provided that the following conditions are
*  met:
*
*  * Redistributions of source code must retain the above copyright
*  notice, this list of conditions and the following disclaimer.
*  * Redistributions in binary form must reproduce the above
*  copyright notice, this list of conditions and the following disclaimer
*  in the documentation and/or other materials provided with the
*  distribution.
*
*  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
*  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
*  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
*  A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
*  OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
*  SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
*  LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
*  DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
*  THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
*  (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
*  OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*
*  You can contact the author at :
*  - xxHash homepage: http://www.xxhash.com
*  - xxHash source repository : https://github.com/Cyan4973/xxHash
*/

//Ported to C# by Ian Qvist
//Source: http://cyan4973.github.io/xxHash/

using System.Runtime.CompilerServices;
using static Genbox.FastHash.XxHash.XxHashConstants;
using static Genbox.FastHash.XxHash.XxHashShared;

namespace Genbox.FastHash.XxHash;

public static class Xx2Hash64
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ComputeIndex(ulong input, ulong seed = 0)
    {
        ulong h64 = seed + PRIME64_5 + 8;
        h64 ^= Round(0, input);
        h64 = RotateLeft(h64, 27) * PRIME64_1 + PRIME64_4;
        return XXH64_avalanche(h64);
    }

    public static ulong ComputeHash(byte[] data, uint seed = 0)
    {
        uint len = (uint)data.Length;
        ulong h64;
        uint offset = 0;

        if (len >= 32)
        {
            uint bEnd = len;
            uint limit = bEnd - 31;
            ulong v1 = seed + PRIME64_1 + PRIME64_2;
            ulong v2 = seed + PRIME64_2;
            ulong v3 = seed + 0;
            ulong v4 = seed - PRIME64_1;

            do
            {
                v1 = Round(v1, Read64(data, offset));
                offset += 8;
                v2 = Round(v2, Read64(data, offset));
                offset += 8;
                v3 = Round(v3, Read64(data, offset));
                offset += 8;
                v4 = Round(v4, Read64(data, offset));
                offset += 8;
            } while (offset < limit);

            h64 = RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);
            h64 = MergeRound(h64, v1);
            h64 = MergeRound(h64, v2);
            h64 = MergeRound(h64, v3);
            h64 = MergeRound(h64, v4);
        }
        else
            h64 = seed + PRIME64_5;

        h64 += len;
        len &= 31;
        while (len >= 8)
        {
            ulong k1 = Round(0, Read64(data, offset));
            offset += 8;
            h64 ^= k1;
            h64 = RotateLeft(h64, 27) * PRIME64_1 + PRIME64_4;
            len -= 8;
        }

        if (len >= 4)
        {
            h64 ^= Read32(data, offset) * PRIME64_1;
            offset += 4;
            h64 = RotateLeft(h64, 23) * PRIME64_2 + PRIME64_3;
            len -= 4;
        }

        while (len > 0)
        {
            h64 ^= data[offset++] * PRIME64_5;
            h64 = RotateLeft(h64, 11) * PRIME64_1;
            len--;
        }

        return XXH64_avalanche(h64);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Round(ulong acc, ulong input)
    {
        acc += input * PRIME64_2;
        acc = RotateLeft(acc, 31);
        acc *= PRIME64_1;
        return acc;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong MergeRound(ulong acc, ulong val)
    {
        val = Round(0, val);
        acc ^= val;
        acc = acc * PRIME64_1 + PRIME64_4;
        return acc;
    }
}