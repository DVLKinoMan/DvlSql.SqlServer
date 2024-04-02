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
    public class Single
    {
        private readonly string TableName = "dbo.Words";

        #region Parameters

        private static readonly object[] ParametersWithFunc =
            new[]
            {
                new object[]
                {
                    (Func<IDataReader, int>) (r => (int) r[0] + 1),
                    new List<int>() {1}, 2
                },
                [
                    (Func<IDataReader, string>) (r => ((string) r[0])[..1]),
                    new List<string>() {"David"}, "D"
                ],
                [
                    (Func<IDataReader, SomeClass>) (r =>
                    {
                        var someClass = new SomeClass( (int)r[r.GetName(0)], (string)r[r.GetName(1)]);
                        return new SomeClass(someClass.SomeIntField + 1,
                            someClass.SomeStringField[..1]);
                    }),
                    new List<SomeClass>()
                        {new (1, "David")},
                    new SomeClass(2, "D")
                ]
            };

        private static readonly object[] ParametersWithoutFunc =
            new[]
            {
                [
                    new List<int>() {1}, 1
                ],
                [
                    new List<string>() {"David"}, "David"
                ],
                new object[]
                {
                    new List<SomeClass>()
                        {new(1, "David")},
                    new SomeClass(1, "David")
                }
            };

        private static readonly object[] ParametersWithoutFuncThrowingException =
            new[]
            {
                [new List<int>()],
                [new List<string>()],
                [new List<SomeClass>()],
                [new List<int>() {1, 2, 3, 4}],
                [new List<string>() {"David", "Lasha"}],
                new object[] {new List<SomeClass>() {new(1, "David"), new(2, "Lasha")}}
            };

        private static readonly object[] ParametersWithFuncThrowingException =
        [
            new object[]
            {
                (Func<IDataReader, int>) (r => (int) r[0] + 1),
                new List<int>()
            },
            new object[]
            {
                (Func<IDataReader, string>) (r => r[0].ToString()![..1]),
                new List<string>()
            }
        ];

        #endregion

        [Test]
        [TestCaseSource(nameof(ParametersWithoutFunc))]
        public void SingleWithoutFunc<T>(List<T> data, T expected)
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
                .SingleAsync<T>()
                .Result;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        [TestCaseSource(nameof(ParametersWithoutFuncThrowingException))]
        public void SingleWithoutFuncThrowsException<T>(List<T> data)
        {
            var readerMoq = CreateDataReaderMock(data);
            var commandMoq = CreateSqlCommandMock<T>(readerMoq);
            var moq = CreateConnectionMock<T>(commandMoq);

            Assert.Throws(Is.InstanceOf(typeof(Exception)), () =>
            {
                var res = new DvlSqlMs(moq.Object)
                    .From(TableName)
                    .Select()
                    .SingleAsync<T>()
                    .Result;
            });
        }

        [Test]
        [TestCaseSource(nameof(ParametersWithFunc))]
        public void SingleWithFunc<T>(Func<IDataReader, T> func, List<T> data, T expected)
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
                .SingleAsync(func)
                .Result;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        [TestCaseSource(nameof(ParametersWithFuncThrowingException))]
        public void SingleWithFuncThrowsException<T>(Func<IDataReader, T> func, List<T> data)
        {
            var readerMoq = CreateDataReaderMock(data);
            var commandMoq = CreateSqlCommandMock<T>(readerMoq);
            var moq = CreateConnectionMock<T>(commandMoq);

            Assert.Throws(Is.InstanceOf(typeof(Exception)), () =>
            {
                var res = new DvlSqlMs(moq.Object)
                    .From(TableName)
                    .Select()
                    .SingleAsync(func)
                    .Result;
            });
        }

        //todo: test with where filter somehow
    }
}