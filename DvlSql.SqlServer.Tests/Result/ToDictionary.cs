using System;
using System.Collections.Generic;
using System.Data;
using DvlSql.SqlServer;
using DvlSql.SqlServer.Classes;
using NUnit.Framework;
using static DvlSql.SqlServer.Result.Helpers;

namespace DvlSql.SqlServer.Result
{
    [TestFixture]
    public class ToDictionary
    {
        private readonly string TableName = "dbo.Words";

        #region Parameters

        private static readonly object[] ParametersWithFunc =
            new[]
            {
                [
                    (Func<IDataReader, int>) (r => (int) r[0]),
                    (Func<IDataReader, int>) (r => (int) r[0] + 1),
                    new List<int>() {1, 1, 2, 2, 3},
                    new Dictionary<int, List<int>>()
                    {
                        {1, new List<int> {2, 2}},
                        {2, new List<int> {3, 3}},
                        {3, new List<int> {4}}
                    }
                ],
                [
                    (Func<IDataReader, string>) (r => ((string) r[0])[..1]),
                    (Func<IDataReader, int>) (r => ((string) r[0]).Length),
                    new List<string>() {"david", "box", "david", "baby", "boy", "barbi", "cable"},
                    new Dictionary<string, List<int>>()
                    {
                        {"d", new List<int>() {5, 5}},
                        {"b", new List<int>() {3, 4, 3, 5}},
                        {"c", new List<int>() {5}},
                    },
                ],
                [
                    (Func<IDataReader, int>) (r => ((string) r[0]).Length),
                    (Func<IDataReader, string>) (r => "Name: " + (string) r[0]),
                    new List<string>() {"david", "box", "david", "baby", "boy", "barbi", "cable"},
                    new Dictionary<int, List<string>>()
                    {
                        {5, new List<string>() {"Name: david", "Name: david", "Name: barbi", "Name: cable"}},
                        {3, new List<string>() {"Name: box", "Name: boy"}},
                        {4, new List<string>() {"Name: baby"}},
                    },
                ],
                new object[]
                {
                    (Func<IDataReader, int>) (r =>
                    {
                        var someClass = new SomeClass((int)r[r.GetName(0)], (string)r[r.GetName(1)]);
                        return someClass.SomeIntField +
                               someClass.SomeStringField.Length;
                    }),
                    (Func<IDataReader, SomeClass>) (r =>
                    {
                        var someClass = new SomeClass((int)r[r.GetName(0)], (string)r[r.GetName(1)]);
                        return new SomeClass(someClass.SomeIntField,
                            someClass.SomeStringField[..1]);
                    }),
                    new List<SomeClass>()
                        {new(1, "David"), new(2, "Lasha"), new(-1, "SomeGuy")},
                    new Dictionary<int, List<SomeClass>>()
                    {
                        {6, new List<SomeClass> {new(1, "D"), new(-1, "S")}},
                        {7, new List<SomeClass> {new(2, "L")}}
                    }
                }
            };

        #endregion

        [Test]
        [TestCaseSource(nameof(ParametersWithFunc))]
        public void TestToDictionary<TKey, TValue, TData>(Func<IDataReader, TKey> keySelector,
            Func<IDataReader, TValue> valueSelector, List<TData> data, Dictionary<TKey, List<TValue>> expected)
        {
            var readerMoq = CreateDataReaderMock(data);
            if (typeof(TData).Namespace != "System")
                foreach (var prop in typeof(TData).GetProperties())
                    if (prop.PropertyType.Namespace == "System")
                    {
                        int ind = -1;
                        readerMoq.Setup(reader => reader[prop.Name])
                                .Callback(() =>
                                {
                                    ind++;
                                })
                                .Returns(() => prop.GetValue(data[ind/2])!);
                    }

            var commandMoq = CreateSqlCommandMock<Dictionary<TKey, List<TValue>>>(readerMoq);
            var moq = CreateConnectionMock<Dictionary<TKey, List<TValue>>>(commandMoq);

            var dictionary = new DvlSqlMs(moq.Object)
                .From(TableName)
                .Select()
                .ToDictionaryAsync(keySelector, valueSelector)
                .Result;

            Assert.That(dictionary, Is.EquivalentTo(expected));
        }
    }
}