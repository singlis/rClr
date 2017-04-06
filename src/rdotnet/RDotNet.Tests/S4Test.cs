﻿using System;
using System.Linq;
using NUnit.Framework;

namespace RDotNet.Tests
{
   [TestFixture]
   class S4Test
   {
      private const string EngineName = "RDotNetTest";

      [TestFixtureSetUp]
      public void SetUpEngine()
      {
         Helper.SetEnvironmentVariables();
         var engine = REngine.CreateInstance(EngineName);
         engine.Initialize();
         engine.Evaluate("setClass('testclass', representation(foo='character', bar='integer'))");
      }

      [TestFixtureTearDown]
      public void DisposeEngine()
      {
         var engine = REngine.GetInstanceFromID(EngineName);
         if (engine != null)
         {
            engine.Dispose();
         }
      }

      [TearDown]
      public void TearDown()
      {
         var engine = REngine.GetInstanceFromID(EngineName);
         engine.Evaluate("rm(list=ls())");
      }

      [Test]
      public void TestGetSlotTypes()
      {
         var engine = REngine.GetInstanceFromID(EngineName);
         var obj = engine.Evaluate("new('testclass', foo='s4', bar=1:4)").AsS4();
         var actual = obj.GetSlotTypes();
         Assert.That(actual.Count, Is.EqualTo(2));
         Assert.That(actual.ContainsKey("foo"), Is.True);
         Assert.That(actual["foo"], Is.EqualTo("character"));
         Assert.That(actual.ContainsKey("bar"), Is.True);
         Assert.That(actual["bar"], Is.EqualTo("integer"));
      }

      [Test]
      public void TestHasSlot()
      {
         var engine = REngine.GetInstanceFromID(EngineName);
         var obj = engine.Evaluate("new('testclass', foo='s4', bar=1:4)").AsS4();
         Assert.That(obj.HasSlot("foo"), Is.True);
         Assert.That(obj.HasSlot("bar"), Is.True);
         Assert.That(obj.HasSlot("baz"), Is.False);
      }

      [Test]
      public void TestGetSlot()
      {
         var engine = REngine.GetInstanceFromID(EngineName);
         var obj = engine.Evaluate("new('testclass', foo='s4', bar=1:4)").AsS4();
         var foo = obj.GetSlot("foo").AsCharacter().First();
         Assert.That(foo, Is.EqualTo("s4"));
         var bar = obj.GetSlot("bar").AsInteger();
         Assert.That(bar, Is.EquivalentTo(new[] { 1, 2, 3, 4 }));
      }

      [Test]
      public void TestSetSlot()
      {
         var engine = REngine.GetInstanceFromID(EngineName);
         var obj = engine.Evaluate("new('testclass', foo='s4', bar=1:4)").AsS4();
         var foo = obj.GetSlot("foo").AsCharacter().First();
         Assert.That(foo, Is.EqualTo("s4"));
         obj.SetSlot("foo", engine.CreateCharacterVector(new[] { "new value" }));
         foo = obj.GetSlot("foo").AsCharacter().First();
         Assert.That(foo, Is.EqualTo("new value"));
      }
   }
}
