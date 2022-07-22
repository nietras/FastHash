﻿//Ported to C# by Ian Qvist
//Source: https://github.com/google/farmhash

namespace Genbox.FastHash.FarmHash;

public static class FarmHash64Unsafe
{
    public static unsafe ulong ComputeHash(byte* s, int len)
    {
        if (len <= 32)
            return len <= 16 ? HashLen0to16(s, len) : HashLen17to32(s, len);

        if (len <= 64)
            return HashLen33to64(s, len);

        if (len <= 96)
            return HashLen65to96(s, len);

        if (len <= 256)
            return Hash64(s, len);

        return Hash64WithSeeds(s, len, 81, 0);
    }

    private static unsafe ulong HashLen0to16(byte* data, int length)
    {
        if (length >= 8)
        {
            ulong mul = FarmHashConstants.k2 + (uint)length * 2;
            ulong a = Utilities.Read64(data) + FarmHashConstants.k2;
            ulong b = Utilities.Read64(data, length - 8);
            ulong c = Utilities.RotateRightCheck(b, 37) * mul + a;
            ulong d = (Utilities.RotateRightCheck(a, 25) + b) * mul;
            return HashLen16(c, d, mul);
        }
        if (length >= 4)
        {
            ulong mul = FarmHashConstants.k2 + (uint)length * 2;
            ulong a = Utilities.Read32(data);
            return HashLen16((uint)length + (a << 3), Utilities.Read32(data, length - 4), mul);
        }
        if (length > 0)
        {
            byte a = data[0];
            byte b = data[length >> 1];
            byte c = data[length - 1];
            uint y = a + ((uint)b << 8);
            uint z = (uint)length + ((uint)c << 2);
            return FarmHashShared.ShiftMix((y * FarmHashConstants.k2) ^ (z * FarmHashConstants.k0)) * FarmHashConstants.k2;
        }
        return FarmHashConstants.k2;
    }

    private static unsafe ulong HashLen17to32(byte* data, int length)
    {
        ulong mul = FarmHashConstants.k2 + (uint)length * 2;
        ulong a = Utilities.Read64(data) * FarmHashConstants.k1;
        ulong b = Utilities.Read64(data, 8);
        ulong c = Utilities.Read64(data, length - 8) * mul;
        ulong d = Utilities.Read64(data, length - 16) * FarmHashConstants.k2;
        return HashLen16(Utilities.RotateRightCheck(a + b, 43) + Utilities.RotateRightCheck(c, 30) + d, a + Utilities.RotateRightCheck(b + FarmHashConstants.k2, 18) + c, mul);
    }

    private static unsafe ulong HashLen33to64(byte* data, int length)
    {
        const ulong mul0 = FarmHashConstants.k2 - 30;
        ulong mul1 = FarmHashConstants.k2 - 30 + 2 * (uint)length;
        ulong h0 = H32(data, 0, 32, mul0);
        ulong h1 = H32(data, length - 32, 32, mul1);
        return (h1 * mul1 + h0) * mul1;
    }

    private static unsafe ulong HashLen65to96(byte* data, int length)
    {
        const ulong mul0 = FarmHashConstants.k2 - 114;
        ulong mul1 = FarmHashConstants.k2 - 114 + 2 * (uint)length;
        ulong h0 = H32(data, 0, 32, mul0);
        ulong h1 = H32(data, 32, 32, mul1);
        ulong h2 = H32(data, length - 32, 32, mul1, h0, h1);
        return (h2 * 9 + (h0 >> 17) + (h1 >> 21)) * mul1;
    }

    private static ulong HashLen16(ulong u, ulong v, ulong mul)
    {
        // Murmur-inspired hashing.
        ulong a = (u ^ v) * mul;
        a ^= a >> 47;
        ulong b = (v ^ a) * mul;
        b ^= b >> 47;
        b *= mul;
        return b;
    }

    private static Uint128 WeakHashLen32WithSeeds(ulong w, ulong x, ulong y, ulong z, ulong a, ulong b)
    {
        a += w;
        b = Utilities.RotateRightCheck(b + a + z, 21);
        ulong c = a;
        a += x;
        a += y;
        b += Utilities.RotateRightCheck(a, 44);
        return new Uint128(a + z, b + c);
    }

    private static unsafe Uint128 WeakHashLen32WithSeeds(byte* data, int offset, ulong a, ulong b) => WeakHashLen32WithSeeds(Utilities.Read64(data, offset),
        Utilities.Read64(data, 8 + offset),
        Utilities.Read64(data, 16 + offset),
        Utilities.Read64(data, 24 + offset),
        a,
        b);

    private static ulong H(ulong x, ulong y, ulong mul, byte r)
    {
        ulong a = (x ^ y) * mul;
        a ^= a >> 47;
        ulong b = (y ^ a) * mul;
        return Utilities.RotateRightCheck(b, r) * mul;
    }

    private static unsafe ulong H32(byte* data, int offset, int length, ulong mul, ulong seed0 = 0, ulong seed1 = 0)
    {
        ulong a = Utilities.Read64(data, offset) * FarmHashConstants.k1;
        ulong b = Utilities.Read64(data, 8 + offset);
        ulong c = Utilities.Read64(data, length - 8 + offset) * mul;
        ulong d = Utilities.Read64(data, length - 16 + offset) * FarmHashConstants.k2;
        ulong u = Utilities.RotateRightCheck(a + b, 43) + Utilities.RotateRightCheck(c, 30) + d + seed0;
        ulong v = a + Utilities.RotateRightCheck(b + FarmHashConstants.k2, 18) + c + seed1;
        a = FarmHashShared.ShiftMix((u ^ v) * mul);
        b = FarmHashShared.ShiftMix((v ^ a) * mul);
        return b;
    }

    private static unsafe ulong Hash64WithSeeds(byte* s, int len, ulong seed0, ulong seed1)
    {
        if (len <= 64)
            return HashLen16(Hash64(s, len) - seed0, seed1, 0x9ddfea08eb382d69UL); //PORT NOTE: This used to refer to Hash128to64, which was the same as HashLen16, just with hardcoded mul

        // For strings over 64 bytes we loop.  Internal state consists of
        // 64 bytes: u, v, w, x, y, and z.
        ulong x = seed0;
        ulong y = seed1 * FarmHashConstants.k2 + 113;
        ulong z = FarmHashShared.ShiftMix(y * FarmHashConstants.k2) * FarmHashConstants.k2;
        Uint128 v = new Uint128(seed0, seed1);
        Uint128 w = new Uint128(0, 0);
        ulong u = x - z;
        x *= FarmHashConstants.k2;
        ulong mul = FarmHashConstants.k2 + (u & 0x82);

        // Set end so that after the loop we have 1 to 64 bytes left to process.
        int index = 0;
        int end = (len - 1) / 64 * 64;
        int last64 = end + ((len - 1) & 63) - 63;
        do
        {
            ulong a0 = Utilities.Read64(s);
            ulong a1 = Utilities.Read64(s, 8);
            ulong a2 = Utilities.Read64(s, 16);
            ulong a3 = Utilities.Read64(s, 24);
            ulong a4 = Utilities.Read64(s, 32);
            ulong a5 = Utilities.Read64(s, 40);
            ulong a6 = Utilities.Read64(s, 48);
            ulong a7 = Utilities.Read64(s, 56);
            x += a0 + a1;
            y += a2;
            z += a3;
            v.Low += a4;
            v.High += a5 + a1;
            w.Low += a6;
            w.High += a7;

            x = Utilities.RotateRightCheck(x, 26);
            x *= 9;
            y = Utilities.RotateRightCheck(y, 29);
            z *= mul;
            v.Low = Utilities.RotateRightCheck(v.Low, 33);
            v.High = Utilities.RotateRightCheck(v.High, 30);
            w.Low ^= x;
            w.Low *= 9;
            z = Utilities.RotateRightCheck(z, 32);
            z += w.High;
            w.High += z;
            z *= 9;
            Utilities.Swap(ref u, ref y);

            z += a0 + a6;
            v.Low += a2;
            v.High += a3;
            w.Low += a4;
            w.High += a5 + a6;
            x += a1;
            y += a7;

            y += v.Low;
            v.Low += x - y;
            v.High += w.Low;
            w.Low += v.High;
            w.High += x - y;
            x += w.High;
            w.High = Utilities.RotateRightCheck(w.High, 34);
            Utilities.Swap(ref u, ref z);
            index += 64;
        } while (index != end);
        // Make s point to the last 64 bytes of input.
        index = last64;
        u *= 9;
        v.High = Utilities.RotateRightCheck(v.High, 28);
        v.Low = Utilities.RotateRightCheck(v.Low, 20);
        w.Low += ((uint)len - 1) & 63;
        u += y;
        y += u;
        x = Utilities.RotateRightCheck(y - x + v.Low + Utilities.Read64(s, index + 8), 37) * mul;
        y = Utilities.RotateRightCheck(y ^ v.High ^ Utilities.Read64(s, index + 48), 42) * mul;
        x ^= w.High * 9;
        y += v.Low + Utilities.Read64(s, index + 40);
        z = Utilities.RotateRightCheck(z + w.Low, 33) * mul;
        v = WeakHashLen32WithSeeds(s, index + 0, v.High * mul, x + w.Low);
        w = WeakHashLen32WithSeeds(s, index + 32, z + w.High, y + Utilities.Read64(s, index + 16));
        return H(HashLen16(v.Low + x, w.Low ^ y, mul) + z - u,
            H(v.High + y, w.High + z, FarmHashConstants.k2, 30) ^ x,
            FarmHashConstants.k2,
            31);
    }

    private static unsafe ulong Hash64(byte* s, int len)
    {
        const ulong seed = 81;

        if (len <= 32)
        {
            if (len <= 16)
                return HashLen0to16(s, len);

            return HashLen17to32(s, len);
        }

        if (len <= 64)
            return HashLen33to64(s, len);

        // For strings over 64 bytes we loop. Internal state consists of 56 bytes: v, w, x, y, and z.
        ulong x = seed;
        ulong y = unchecked(seed * FarmHashConstants.k1) + 113;
        ulong z = FarmHashShared.ShiftMix(y * FarmHashConstants.k2 + 113) * FarmHashConstants.k2;
        Uint128 v = new Uint128(0, 0);
        Uint128 w = new Uint128(0, 0);
        x = x * FarmHashConstants.k2 + Utilities.Read64(s);

        // Set end so that after the loop we have 1 to 64 bytes left to process.
        int index = 0;
        int end = (len - 1) / 64 * 64;
        int last64 = end + ((len - 1) & 63) - 63;
        do
        {
            x = Utilities.RotateRightCheck(x + y + v.Low + Utilities.Read64(s, 8), 37) * FarmHashConstants.k1;
            y = Utilities.RotateRightCheck(y + v.High + Utilities.Read64(s, 48), 42) * FarmHashConstants.k1;
            x ^= w.High;
            y += v.Low + Utilities.Read64(s, 40);
            z = Utilities.RotateRightCheck(z + w.Low, 33) * FarmHashConstants.k1;
            v = WeakHashLen32WithSeeds(s, 0, v.High * FarmHashConstants.k1, x + w.Low);
            w = WeakHashLen32WithSeeds(s, 32, z + w.High, y + Utilities.Read64(s, 16));
            Utilities.Swap(ref z, ref x);
            index += 64;
        } while (index != end);

        ulong mul = FarmHashConstants.k1 + ((z & 0xff) << 1);
        // Make s point to the last 64 bytes of input.
        index = last64;
        w.Low += ((uint)len - 1) & 63;
        v.Low += w.Low;
        w.Low += v.Low;
        x = Utilities.RotateRightCheck(x + y + v.Low + Utilities.Read64(s, index + 8), 37) * mul;
        y = Utilities.RotateRightCheck(y + v.High + Utilities.Read64(s, index + 48), 42) * mul;
        x ^= w.High * 9;
        y += v.Low * 9 + Utilities.Read64(s, index + 40);
        z = Utilities.RotateRightCheck(z + w.Low, 33) * mul;
        v = WeakHashLen32WithSeeds(s, index + 0, v.High * mul, x + w.Low);
        w = WeakHashLen32WithSeeds(s, index + 32, z + w.High, y + Utilities.Read64(s, index + 16));
        Utilities.Swap(ref z, ref x);
        return HashLen16(HashLen16(v.Low, w.Low, mul) + FarmHashShared.ShiftMix(y) * FarmHashConstants.k0 + z,
            HashLen16(v.High, w.High, mul) + x,
            mul);
    }
}