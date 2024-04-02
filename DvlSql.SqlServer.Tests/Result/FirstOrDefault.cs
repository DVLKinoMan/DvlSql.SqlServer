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
    public class FirstOrDefault
    {
        private readonly string TableName = "dbo.Words";

        #region Parameters

        private static readonly object[] ParametersForFirstOrDefaultWithFunc =
            new[]
            {
                new object[]
                {
                    (Func<IDataReader, int>) (r => (int) r[0] + 1),
                    new List<int>() {1, 2, 3}, 2
                },
                [
                    (Func<IDataReader, string>) (r => ((string) r[0])[..1]),
                    new List<string>() {"David", "Lasha", "SomeGuy"}, "D"
                ],
                [
                    (Func<IDataReader, SomeClass>) (r =>
                    {
                        var someClass = new SomeClass((int)r[r.GetName(0)], (string)r[r.GetName(1)]);
                        return new SomeClass(someClass.SomeIntField + 1,
                            someClass.SomeStringField[..1]);
                    }),
                    new List<SomeClass>()
                        {new(1, "David"), new(2, "Lasha"), new(3, "SomeGuy")},
                    new SomeClass(2, "D")
                ],
                [
                    (Func<IDataReader, int>) (r => (int) r[0] + 1),
                    new List<int>(),
                    default(int)
                ],
                [
                    (Func<IDataReader, string>) (r => r[0].ToString()![..1]),
                    new List<string>(),
                    null
                ]
            };

        private static readonly object[] ParametersForFirstOrDefaultWithoutFunc =
            new[]
            {
                [new List<int>(), null],
                [new List<string>(), null],
                [new List<int>() {1, 2, 3, 4, 5, 15}, 1],
                new object[] {new List<int>() {15, 5, 4, 3, 2, 1}, 15}
            };

        #endregion

        [Test]
        [TestCaseSource(nameof(ParametersForFirstOrDefaultWithoutFunc))]
        public void FirstOrDefaultWithoutFunc<T>(List<T> data, T expected)
        {
            var readerMoq = CreateDataReaderMock(data);
            var commandMoq = CreateSqlCommandMock<T>(readerMoq);
            var moq = CreateConnectionMock<T>(commandMoq);

            var actual = new DvlSqlMs(moq.Object)
                .From(TableName)
                .Select()
                .FirstOrDefaultAsync<T>()
                .Result;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        [TestCaseSource(nameof(ParametersForFirstOrDefaultWithFunc))]
        public void FirstOrDefaultWithFunc<T>(Func<IDataReader, T> func, List<T> data, T expected)
        {
            var readerMoq = CreateDataReaderMock(data);
            if (typeof(T).Namespace != "System")
                foreach (var prop in typeof(T).GetProperties())
                    if (prop.PropertyType.Namespace == "System")
                        readerMoq.Setup(reader => reader[prop.Name])
                            .Returns(() => prop.GetValue(data[0])!);
            var commandMoq = CreateSqlCommandMock<T>(readerMoq);
            var moq = CreateConnectionMock<T>(commandMoq);

            var actual = new DvlSqlMs(moq.Object)
                .From(TableName)
                .Select()
                .FirstOrDefaultAsync(func)
                .Result;

            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}