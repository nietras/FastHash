﻿using BenchmarkDotNet.Running;

namespace Genbox.FastHash.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly);
    }
}