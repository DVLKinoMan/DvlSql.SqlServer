﻿using System.Text.RegularExpressions;
using System.Threading.Tasks;

using DvlSql.SqlServer.Result;
using NUnit.Framework;

using static DvlSql.ExpressionHelpers;
using static DvlSql.Extensions.SqlType;
using static DvlSql.Extensions.DataReader;
using DvlSql.SqlServer;

namespace DvlSql.SqlServer.Transaction
{
    [TestFixture]
    class Transactions
    {
        private readonly DvlSqlMs _sql =
            new (@"Data Source=LAPTOP-DEUOP46M\LOCALHOST; Initial Catalog=DVL_Test; Connection Timeout=30; Application Name = DVLSqlTest1");

        //todo normal test
        //[Test]
        public async Task TestMethod1()
        {
            var conn = await this._sql.BeginTransactionAsync();
            await this._sql.SetConnection(conn).DeleteFrom("dbo.Words")
                .ExecuteAsync();
            _ = await this._sql.SetConnection(conn).InsertInto<(int, string)>("dbo.Words",
                    IntType("Id"), NVarCharType("Name", 50))
                .Values((1, "Some New Word"), (2, "Some New Word 2"))
                .ExecuteAsync();
            _ = await this._sql.SetConnection(conn).Update("dbo.Words")
                .Set(NVarChar("Name", "Updated Word", 50))
                .Where(ConstantExpCol("Id") == 1)
                .ExecuteAsync();

            await this._sql.SetConnection(conn).CommitAsync();
        }


        //todo normal test
        //[Test]
        public async Task TestMethod2()
        {
            var table = this._sql.DeclareTable("inserted")
                .AddColumns(IntType("id", true));

            var conn = await this._sql.BeginTransactionAsync();
            await this._sql.SetConnection(conn).DeleteFrom("dbo.Words")
                .ExecuteAsync();

            var k = await this._sql.SetConnection(conn).InsertInto<(int, string)>("dbo.Words",
                    IntType("Id"), NVarCharType("Name", 50))
                .Output(AsList (r=>int.Parse(r["id"].ToString()!)),"inserted.id")
                .Values((1, "Some New Word"), (2, "Some New Word 2"))
                .ExecuteAsync();

            await this._sql.SetConnection(conn).CommitAsync();
        }
    }
}
