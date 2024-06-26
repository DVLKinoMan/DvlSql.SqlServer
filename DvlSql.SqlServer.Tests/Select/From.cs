﻿using NUnit.Framework;
using System;
using System.Text.RegularExpressions;
using static DvlSql.ExpressionHelpers;

namespace DvlSql.SqlServer.Select
{
    [TestFixture]
    public class From
    {
        private readonly DvlSqlMs _sql =
            new (
                StaticConnectionStrings.ConnectionStringForTest);

        [Test]
        [TestCase("dbo.Words")]
        public void WithoutSelect(string tableName)
        {
            var from = this._sql.From(tableName).ToString();

            var expectedSelect = Regex.Escape($"SELECT * FROM {tableName}");
            Assert.That(Regex.Escape(from!), Is.EqualTo(expectedSelect));
        }

        [Test]
        [TestCase("dbo.Words")]
        public void WithoutNoLock(string tableName)
        {
            var actualSelect1 = this._sql.From(tableName)
                .Select()
                .ToString();
            
            var actualSelect2 = this._sql.From(tableName, false)
                .Select()
                .ToString();

            var expectedSelect = Regex.Escape($"SELECT * FROM {tableName}");
            Assert.Multiple(() =>
            {
                Assert.That(Regex.Escape(actualSelect1!), Is.EqualTo(expectedSelect));
                Assert.That(Regex.Escape(actualSelect2!), Is.EqualTo(expectedSelect));
            });
        }

        [Test]
        [TestCase("dbo.Words")]
        public void WithNoLock(string tableName)
        {
            var actualSelect = this._sql.From(tableName, true)
                .Select()
                .ToString();

            var expectedSelect = Regex.Escape($"SELECT * FROM {tableName} WITH(NOLOCK)");
            Assert.That(Regex.Escape(actualSelect!), Is.EqualTo(expectedSelect));
        }

        [Test]
        [TestCase("dbo.Words")]
        public void WithInnerSelect(string tableName)
        {
            ArgumentNullException.ThrowIfNull(tableName);

            var asName = "W";
            var fullSelect = FullSelectExp(SelectExp(), FromExp("dbo.Words"), @as: AsExp(asName));
            var actualSelect = this._sql.From(fullSelect)
                .Select()
                .ToString();

            var expectedSelect = Regex.Escape($"SELECT * FROM (SELECT * FROM dbo.Words) AS {asName}");
            Assert.That(Regex.Escape(actualSelect!), Is.EqualTo(expectedSelect));
        }
    }
}