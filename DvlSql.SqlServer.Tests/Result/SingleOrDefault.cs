using DvlSql.SqlServer.Classes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using static DvlSql.SqlServer.Result.Helpers;

namespace DvlSql.SqlServer.Result
{
    [TestFixture]
    public class SingleOrDefault
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
                    (Func<IDataReader, string>) (r => ((string) r[0])[..1]),
                    new List<string>() {"David", "Lasha"}, null
                ],
                [
                    (Func<IDataReader, SomeClass>) (r =>
                    {
                        var someClass = new SomeClass((int)r[r.GetName(0)], (string)r[r.GetName(1)]);
                        return new SomeClass(someClass.SomeIntField + 1,
                            someClass.SomeStringField[..1]);
                    }),
                    new List<SomeClass>()
                        {new(1, "David")},
                    new SomeClass(2, "D")
                ],
                [
                    (Func<IDataReader, SomeClass>) (r =>
                    {
                        var someClass = new SomeClass((int)r[r.GetName(0)], (string)r[r.GetName(1)]);
                        return new SomeClass(someClass.SomeIntField + 1,
                            someClass.SomeStringField[..1]);
                    }),
                    new List<SomeClass>()
                        {new(1, "David"), new(2, "Lasga")},
                    null
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
                [
                    new List<string>() {"David", "Lasha"}, default(string)
                ],
                [
                    new List<SomeClass>()
                        {new(1, "David")},
                    new SomeClass(1, "David")
                ],
                new object[]
                {
                    new List<SomeClass>()
                        {new(1, "David"), new(2, "Lasha")},
                    default(SomeClass)
                }
            };

        #endregion

        [Test]
        [TestCaseSource(nameof(ParametersWithoutFunc))]
        public void SingleOrDefaultWithoutFunc<T>(List<T> data, T expected)
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
                .SingleOrDefaultAsync<T>()
                .Result;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        [TestCaseSource(nameof(ParametersWithFunc))]
        public void SingleOrDefaultWithFunc<T>(Func<IDataReader, T> func, List<T> data, T expected)
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
                .SingleOrDefaultAsync(func)
                .Result;

            Assert.That(actual, Is.EqualTo(expected));
        }
        
    }
}