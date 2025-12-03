// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Helper.Generic;
using System.Collections.Generic;
using Xunit;

namespace OdhApiCoreTests.Helper
{

    public class SetNestedPropertyValueToNullTests
    {
        [Fact]
        public void Sets_Value_On_Single_Array_Element_To_Null()
        {
            var obj = new TestRootObject
            {
                Nested = new TestNestedObject
                {
                    Array = new List<TestArrayObject>
                {
                    new TestArrayObject { Value = "Hello" }
                }
                }
            };

            obj.SetNestedPropertyValueToNull("Nested.Array.[].Value");

            Assert.Null(obj.Nested.Array[0].Value);
        }

        [Fact]
        public void Sets_Value_On_All_Array_Elements_To_Null()
        {
            var obj = new TestRootObject
            {
                Nested = new TestNestedObject
                {
                    Array = new List<TestArrayObject>
                {
                    new TestArrayObject { Value = "A" },
                    new TestArrayObject { Value = "B" },
                    new TestArrayObject { Value = "C" }
                }
                }
            };

            obj.SetNestedPropertyValueToNull("Nested.Array.[].Value");

            Assert.Null(obj.Nested.Array[0].Value);
            Assert.Null(obj.Nested.Array[1].Value);
            Assert.Null(obj.Nested.Array[2].Value);
        }

        [Fact]
        public void Does_Not_Fail_If_Target_Property_Does_Not_Exist()
        {
            var obj = new TestRootObject
            {
                Nested = new TestNestedObject
                {
                    Array = new List<TestArrayObject>
                {
                    new TestArrayObject { Value = "NotChanged" }
                }
                }
            };

            obj.SetNestedPropertyValueToNull("Nested.Array.[].UnknownProperty");

            Assert.Equal("NotChanged", obj.Nested.Array[0].Value);
        }

        [Fact]
        public void Does_Not_Throw_If_Intermediate_Object_Is_Null()
        {
            var obj = new TestRootObject
            {
                Nested = null
            };

            obj.SetNestedPropertyValueToNull("Nested.Array.[].Value");

            Assert.Null(obj.Nested);
        }

        [Fact]
        public void Handles_Null_Elements_Inside_Array()
        {
            var obj = new TestRootObject
            {
                Nested = new TestNestedObject
                {
                    Array = new List<TestArrayObject>
                {
                    new TestArrayObject { Value = "A" },
                    null,
                    new TestArrayObject { Value = "C" }
                }
                }
            };

            obj.SetNestedPropertyValueToNull("Nested.Array.[].Value");

            Assert.Null(obj.Nested.Array[0].Value);
            Assert.Null(obj.Nested.Array[2].Value);
        }

        [Fact]
        public void Handles_Deeply_Nested_Array_Path()
        {
            var obj = new TestRootObject
            {
                Nested = new TestNestedObject
                {
                    Array = new List<TestArrayObject>
                {
                    new TestArrayObject { Value = "DeepTest" }
                }
                }
            };

            obj.SetNestedPropertyValueToNull("Nested.Array.[].Value");

            Assert.Null(obj.Nested.Array[0].Value);
        }

        [Fact]
        public void Sets_Root_Level_Property_To_Null()
        {
            var obj = new TestRootObject
            {
                RootValue = "Initial"
            };

            obj.SetNestedPropertyValueToNull("RootValue");

            Assert.Null(obj.RootValue);
        }

        [Fact]
        public void Sets_Nested_Object_Property_To_Null()
        {
            var obj = new TestRootObject
            {
                Nested = new TestNestedObject
                {
                    NestedValue = "ShouldBeNull"
                }
            };

            obj.SetNestedPropertyValueToNull("Nested.NestedValue");

            Assert.Null(obj.Nested.NestedValue);
        }
    }


    public class TestArrayObject
    {
        public string Value { get; set; }
    }

    public class TestNestedObject
    {
        public List<TestArrayObject> Array { get; set; }
        public string NestedValue { get; set; }  
    }

    public class TestRootObject
    {
        public TestNestedObject Nested { get; set; }
        public string RootValue { get; set; }
    }
}
