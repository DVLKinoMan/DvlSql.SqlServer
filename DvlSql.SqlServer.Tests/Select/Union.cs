using System;
using System.Text.RegularExpressions;
using DvlSql.SqlServer;
using NUnit.Framework;

namespace DvlSql.SqlServer.Select
{
    [TestFixture]
    public class Union
    {
        private readonly DvlSqlMs _sql =
            new (
                StaticConnectionStrings.ConnectionStringForTest);

        [Test]
        [TestCase("dbo.Words", "dbo.Sentences")]
        public void Select_With_Union(string table1, string table2)
        {
            var actualSelect = this._sql
                .From(table1)
                .Select()
                .Union()
                .From(table2)
                .Select()
                .ToString();

            string expectedSelect = Regex.Escape(string.Format(
                "SELECT * FROM {1}{0}" +
                "UNION{0}" +
                "SELECT * FROM {2}",
                Environment.NewLine,
                table1,
                table2));

            Assert.That(Regex.Escape(actualSelect!), Is.EqualTo(expectedSelect));
        }

        [Test]
        [TestCase("dbo.Words", "dbo.Sentences")]
        public void Select_With_UnionAll(string table1, string table2)
        {
            var actualSelect = this._sql
                .From(table1)
                .Select()
                .UnionAll()
                .From(table2)
                .Select()
                .ToString();

            string expectedSelect = Regex.Escape(string.Format(
                "SELECT * FROM {1}{0}" +
                "UNION ALL{0}" +
                "SELECT * FROM {2}",
                Environment.NewLine,
                table1,
                table2));

            Assert.That(Regex.Escape(actualSelect!), Is.EqualTo(expectedSelect));
        }


        [Test]
        [TestCase("dbo.Words", "dbo.Sentences")]
        public void Select_With_UnionAndUnionAllCombinations(string table1, string table2)
        {
            var select = this._sql
                .From(table1)
                .Select();

            var firstUnion = select.Union()
                .From(table2)
                .Select();

            var firstUnionAll = select.UnionAll()
                .From(table2)
                .Select();
            _ = firstUnion.UnionAll()
                .From(table1)
                .Select()
                .ToString();
            _ = firstUnionAll.Union()
                .From(table1)
                .Select()
                .ToString();
            _ = Regex.Escape(string.Format(
                "SELECT * FROM {1}{0}" +
                "UNION{0}" +
                "SELECT * FROM {2}{0}" +
                "UNION ALL{0}" +
                "SELECT * FROM {1}",
                Environment.NewLine,
                table1,
                table2));
            _ = Regex.Escape(string.Format(
                "SELECT * FROM {1}{0}" +
                "UNION ALL{0}" +
                "SELECT * FROM {2}{0}" +
                "UNION{0}" +
                "SELECT * FROM {1}",
                Environment.NewLine,
                table1,
                table2));

            //todo: problem because it Union changes state
            // Assert.Multiple(() =>
            // {
            //     Assert.That(Regex.Escape(actualSelect1), Is.EqualTo(expectedSelect1));
            //     Assert.That(Regex.Escape(actualSelect2), Is.EqualTo(expectedSelect2));
            // });
        }
    }
}