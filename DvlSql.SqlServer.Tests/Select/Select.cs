﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using DvlSql.SqlServer;
using NUnit.Framework;

namespace DvlSql.SqlServer.Select
{
    [TestFixture]
    public class Select
    {
        
        private readonly DvlSqlMs _sql =
            new (
                StaticConnectionStrings.ConnectionStringForTest);

        private readonly string TableName = "dbo.Words";

        [Test]
        public void SelectAll()
        {
            var actualSelect = _sql.From(TableName)
                .Select()
                .ToString();

            var expectedSelect = Regex.Escape($"SELECT * FROM {TableName}");

            Assert.That(Regex.Escape(actualSelect!), Is.EqualTo(expectedSelect));
        }
        
        [Test]
        [TestCase(null)]
        [TestCase("Id", "Name", "CreatedDate")]
        public void SelectWithParameters(params string[] fields)
        {
            var select = _sql.From(TableName)
                .Select(fields);

            //if (fields.Any(string.IsNullOrEmpty))
            //{
            //    Assert.Throws<ArgumentException>(() => select.ToString());
            //    return;
            //}

            var actualSelect = select.ToString();
            Console.WriteLine(actualSelect);
            var expectedSelect = Regex.Escape($"SELECT {(fields == null || fields.Length == 0 ? "*" : string.Join(", ", fields))} FROM {TableName}");

            Assert.That(Regex.Escape(actualSelect!), Is.EqualTo(expectedSelect));
        }

        [Test]
        [TestCase(10)]
        [TestCase(10, "Id", "Name", "CreatedDate")]
        public void SelectWithTop(int topNum, params string[] fields)
        {
            var actualSelect = _sql.From(TableName)
                .SelectTop(topNum, fields)
                .ToString();

            var expectedSelect = Regex.Escape($"SELECT TOP {topNum} {(fields.Length == 0? $"*" : string.Join(", ", fields))} FROM {TableName}");
            
            Assert.That(Regex.Escape(actualSelect!), Is.EqualTo(expectedSelect));
        }
    }
}