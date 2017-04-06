﻿using NUnit.Framework;

namespace RDotNet
{
    public class S4ClassesTest : RDotNetTestFixture
    {
        [Test]
        public void TestSlots()
        {
            var engine = this.Engine;

            engine.Evaluate("track <- setClass('track', slots = c(x='numeric', y='numeric'))");
            var t1 = engine.Evaluate("track(x = 1:10, y = 1:10 + rnorm(10))").AsS4();

            Assert.AreEqual(true, t1.HasSlot("x"));
            Assert.AreEqual(false, t1.HasSlot("X"));
            Assert.AreEqual(new[] { "x", "y" }, t1.SlotNames);

            double[] x = t1["x"].AsNumeric().ToArray();
            var expx = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            Assert.AreEqual(expx, x);
            double[] y = t1["y"].AsNumeric().ToArray();
            y[2] = 0.1;
            t1["y"] = engine.CreateNumericVector(y);
            Assert.AreEqual(y, t1["y"].AsNumeric().ToArray());
        }

        [Test]
        public void TestGetSlotTypes()
        {
            var engine = this.Engine;
            engine.Evaluate("setClass('testclass', representation(foo='character', bar='integer'))");
            var obj = engine.Evaluate("new('testclass', foo='s4', bar=1:4)").AsS4();
            var actual = obj.GetSlotTypes();
            Assert.That(actual.Count, Is.EqualTo(2));
            Assert.That(actual.ContainsKey("foo"), Is.True);
            Assert.That(actual["foo"], Is.EqualTo("character"));
            Assert.That(actual.ContainsKey("bar"), Is.True);
            Assert.That(actual["bar"], Is.EqualTo("integer"));
        }
    }
}