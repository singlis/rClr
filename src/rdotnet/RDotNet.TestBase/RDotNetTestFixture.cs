﻿using NUnit.Framework;
using RDotNet.NativeLibrary;
using System;
using System.Linq;
using System.Numerics;

namespace RDotNet
{
    [TestFixture]
    public class RDotNetTestFixture
    {
        private static readonly MockDevice device = new MockDevice();

        //      protected string EngineName { get { return "RDotNetTest"; } }

        protected MockDevice Device { get { return device; } }

        private static REngine engine = null;

        protected REngine Engine { get { return engine; } }

        private readonly bool initializeOnceOnly = true;

        protected static void ReportFailOnLinux(string additionalMsg)
        {
            if (NativeUtility.IsUnix)
                throw new NotSupportedException("This unit test is problematic to run from NUnit on Linux " + additionalMsg);
        }

        [TestFixtureSetUp]
        protected virtual void SetUpFixture()
        {
            if (initializeOnceOnly && engine != null)
                return;
            REngine.SetEnvironmentVariables();
            engine = REngine.GetInstance(dll: null, initialize: true, parameter: null, device: Device);
        }

        [TestFixtureTearDown]
        protected virtual void TearDownFixture()
        {
            if (engine != null)
                engine.ClearGlobalEnvironment();
        }

        [SetUp]
        protected virtual void SetUpTest()
        {
            engine.Evaluate("rm(list=ls())");
            this.Device.Initialize();
        }

        [TearDown]
        protected virtual void TearDownTest()
        {
        }

        protected static double GetRMemorySize(REngine engine)
        {
            var memoryAfterAlloc = engine.Evaluate("memory.size()").AsNumeric().First();
            return memoryAfterAlloc;
        }

        protected static void GarbageCollectRandClr(REngine engine)
        {
            // it seems needed to call gc() twice to get a proper baseline.
            REngine.DoDotNetGarbageCollection();
            engine.ForceGarbageCollection();
            REngine.DoDotNetGarbageCollection();
            engine.ForceGarbageCollection();
        }

        protected double[] GenArrayDouble(int from, int to)
        {
            return Array.ConvertAll(GenArrayInteger(from, to), input => (double)input);
        }

        protected static T[,] ToMatrix<T>(T[] d, int nrow, int ncol)
        {
            var res = new T[nrow, ncol];
            for (int i = 0; i < nrow; i++)
                for (int j = 0; j < ncol; j++)
                    res[i, j] = d[nrow * j + i];
            return res;
        }

        protected double[] ArrayMult(double[] a, double mult)
        {
            return Array.ConvertAll(a, input => input * mult);
        }

        protected int[] GenArrayInteger(int from, int to)
        {
            Assert.Greater(to, from);
            var res = new int[to - from + 1];
            for (int i = 0; i < (to - from + 1); i++)
                res[i] = i + from;
            return res;
        }

        // I thought NUnit was dealing with array equivalence. Cannot see here, so emulate.
        protected static void CheckArrayEqual<T>(T[] a, T[] expected)
        {
            Assert.AreEqual(expected.Length, a.Length);
            for (int i = 0; i < a.Length; i++)
                AssertElementsAreEqual(expected[i], a[i]); //, 1e-9);
        }

        public static void AssertComplexAreEqual(Complex actual, Complex expected)
        {
            // TestCreateComplexValid test otherwise fails on Mono, for NA values for R complex vectors. 
            if (double.IsNaN(expected.Real))
                Assert.True(double.IsNaN(actual.Real));
            if (double.IsNaN(expected.Imaginary))
                Assert.True(double.IsNaN(actual.Imaginary));
            if (!double.IsNaN(expected.Real) && !double.IsNaN(expected.Imaginary))
                Assert.AreEqual(expected, actual);
        }

        protected static void AssertElementsAreEqual<T>(T actual, T expected)
        {
            // Yuck, but 
            // TestCreateComplexValid test otherwise fails on Mono, for NA values for R complex vectors. 
            if (typeof(T) == typeof(Complex))
                AssertComplexAreEqual((Complex)(object)actual, (Complex)(object)expected);
           else
                Assert.AreEqual(expected, actual);
        }

        // I thought NUnit was dealing with array equivalence. Cannot see here, so emulate.
        protected static void CheckArrayEqual<T>(T[,] a, T[,] expected)
        {
            Assert.AreEqual(expected.Length, a.Length);
            for (int i = 0; i < 2; i++)
                Assert.AreEqual(expected.GetLength(i), a.GetLength(i));
            for (int i = 0; i < a.GetLength(0); i++)
                for (int j = 0; j < a.GetLength(1); j++)
                    Assert.AreEqual(expected[i, j], a[i, j]); //, 1e-9);
        }
    }
}