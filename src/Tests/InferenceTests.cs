using NUnit.Framework;
using Overby.LINQPad.FileDriver.TypeInference;
using System.Collections.Generic;
using System.Linq;
using static Overby.LINQPad.FileDriver.TypeInference.BestType;

namespace Tests
{
    public class InferenceTests
    {
        [Test]
        public void _1_2_3_Bytes()
        {
            AssertBestType(Byte, 1, 2, 3);
        }

        [Test]
        public void Bytes_Int16_Int16()
        {
            AssertBestType(Int16, 1, 2, 3, byte.MaxValue + 1);
        }

        [Test]
        public void Bytes_Int32_Int32()
        {
            AssertBestType(Int32, 1, 2, 3, short.MaxValue + 1);
        }

        [Test]
        public void Bytes_Int32_Int64()
        {
            AssertBestType(Int64, 1, 2, 3, (long)int.MaxValue + 1);
        }

        [Test]
        public void Byte_BitInt_BigInt()
        {
            AssertBestType(BigInt, 1, decimal.MaxValue + "0");
        }

        [Test]
        public void Byte_A_Char()
        {
            AssertBestType(Char, 1, "A");
        }

        [Test]
        public void Byte_Empty_NullableBool()
        {
            AssertBestType(NullableBool, 1, "");
        }

        [Test]
        public void One_Bool()
        {
            AssertBestType(Bool, 1);
        }

        [Test]
        public void Zero_Bool()
        {
            AssertBestType(Bool, 0);
        }

        [Test]
        public void One_Zero_Bool()
        {
            AssertBestType(Bool, 1, 0);
        }

        [Test]
        public void One_Zero_Empty_NullableBool()
        {
            AssertBestType(NullableBool, 1, 0, "");
        }

        [Test]
        public void Empty_String()
        {
            AssertBestType(String, "");
        }

        [Test]
        public void One_Dot_One_Decimal()
        {
            AssertBestType(Decimal, "1.1");
        }

        [Test]
        public void Decimal_Epsilon_Double()
        {
            AssertBestType(Double, "1.1", double.Epsilon);
        }

        [Test]
        public void Decimal_PositiveInfinity_Double()
        {
            AssertBestType(Double, "1.1", double.PositiveInfinity);
        }

        [Test]
        public void Decimal_NegativeInfinity_Double()
        {
            AssertBestType(Double, "1.1", double.NegativeInfinity);
        }

        [Test]
        public void Decimal_NaN_Double()
        {
            AssertBestType(Double, "1.1", double.NaN);
        }

        [Test]
        public void Numerics1()
        {
            AssertBestType(Double, 0, 1, byte.MaxValue, short.MaxValue, int.MaxValue, long.MaxValue, decimal.MaxValue, double.MaxValue);
        }

        [Test]
        public void Numerics2()
        {
            AssertBestType(Decimal, 0, 1, byte.MaxValue, short.MaxValue, int.MaxValue, long.MaxValue, decimal.MaxValue);
        }

        [Test]
        public void Numerics3()
        {
            AssertBestType(Int64, 0, 1, byte.MaxValue, short.MaxValue, int.MaxValue, long.MaxValue);
        }

        [Test]
        public void Numerics4()
        {
            AssertBestType(Int32, 0, 1, byte.MaxValue, short.MaxValue, int.MaxValue);
        }

        [Test]
        public void Numerics5()
        {
            AssertBestType(Int16, 0, 1, byte.MaxValue, short.MaxValue);
        }

        [Test]
        public void Numerics6()
        {
            AssertBestType(Byte, 0, 1, byte.MaxValue);
        }

        [Test]
        public void Decimal_Concat_Zero_BigInt()
        {
            AssertBestType(BigInt, decimal.MaxValue + "0");
        }

        [Test]
        public void Decimal_Concat_Pt_One_Decimal()
        {
            // decimal rounds values

            AssertBestType(Decimal, decimal.MaxValue + ".1");
        }

        [Test]
        public void DateTime1()
        {
            AssertBestType(DateTime, "5/10/1984");
        }

        [Test]
        public void DateTime2()
        {
            AssertBestType(DateTime, "1984/5/10");
        }

        [Test]
        public void DateTime3()
        {
            AssertBestType(DateTime, System.DateTime.Now);
        }

        [Test]
        public void DateTime4()
        {
            AssertBestType(DateTime, System.DateTime.Now, System.DateTime.Now);
        }

        [Test]
        public void DateTimeOffset1()
        {
            AssertBestType(DateTime, System.DateTimeOffset.Now);
        }

        [Test]
        public void DateTimeOffset2()
        {
            AssertBestType(DateTime, System.DateTime.Now, System.DateTimeOffset.Now);
        }

        [Test]
        public void TimeSpan1()
        {
            AssertBestType(Timespan, System.DateTime.Now - System.DateTimeOffset.Now);
        }

        [Test]
        public void TimeSpanMax()
        {
            AssertBestType(Timespan, System.TimeSpan.MaxValue);
        }

        [Test]
        public void TimeSpanMin()
        {
            AssertBestType(Timespan, System.TimeSpan.MinValue);
        }

        [Test]
        public void TimeSpan1Day()
        {
            AssertBestType(Timespan, System.TimeSpan.FromDays(1));
        }

        [Test]
        public void NewGuid()
        {
            AssertBestType(Guid, System.Guid.NewGuid());
        }

        [Test]
        public void EmptyGuid()
        {
            AssertBestType(Guid, System.Guid.Empty);
        }

        [Test]
        public void NullGuid()
        {
            AssertBestType(NullableGuid, System.Guid.Empty, "");
        }

        [Test]
        public void NudeGuid()
        {
            AssertBestType(Guid, System.Guid.NewGuid().ToString("n"));
        }

        [Test]
        public void DeltaBurkeInvadesAGuid()
        {
            AssertBestType(String, System.Guid.NewGuid().ToString("n"), "delta burke");
        }

        [Test]
        public void MixedUp1()
        {
            AssertBestType(String, System.Guid.NewGuid(), 1, -1, 0, true, false, System.DateTime.Now);
        }

        [Test]
        public void MixedBools()
        {
            AssertBestType(Bool, 1, true);
            AssertBestType(Bool, 0, false);
            AssertBestType(Bool, 1, true, 0, false);
            AssertBestType(Bool, 0, true);
            AssertBestType(Bool, 1, false);
            AssertBestType(NullableBool, 1, false, " ");
        }

        #region Test Stuff
        void AssertBestType(BestType expected, params object[] values) =>
          Assert.AreEqual(expected, DetermineBestTypes(values));

        BestType DetermineBestTypes(params object[] values)
        {
            var bestTypes = TypeInferrer.DetermineBestTypes(Data(values));
            return bestTypes[_key];
        }

        const int _key = 0;

        static IEnumerable<IEnumerable<(int, string)>> Data(params object[] values) =>
            values.Select(v => new[] { (_key, v?.ToString() ?? "") });

        #endregion

    }
}
