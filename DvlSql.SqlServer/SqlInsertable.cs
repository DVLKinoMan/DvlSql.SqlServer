using DvlSql.Expressions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using static DvlSql.ExpressionHelpers;
using static DvlSql.Extensions.SqlType;
using static System.Exts.Extensions;

namespace DvlSql.SqlServer
{
    internal class BaseInsertable<TParam>(DvlSqlInsertExpression insertIntoExpression, IDvlSqlConnection conn) where TParam : ITuple
    {
        protected DvlSqlInsertExpression InsertIntoExpression = insertIntoExpression;
        protected readonly IDvlSqlConnection DvlSqlConnection = conn;

        protected void SetOutputExpression(DvlSqlTableDeclarationExpression intoTable, string[] cols)
        {
            this.InsertIntoExpression.OutputExpression = OutputExp(intoTable, cols);
        }

        protected void SetOutputExpression(string[] cols)
        {
            this.InsertIntoExpression.OutputExpression = OutputExp(cols);
        }

        protected IInsertDeleteExecutable<int> Values(params TParam[] @params)
        {
            var insertInto = this.InsertIntoExpression as DvlSqlInsertIntoExpression<TParam> 
                             ?? throw new Exception("Can not Convert to Generic insert Expression");
            insertInto.ValuesExpression = ValuesExp(@params);
            insertInto.WithParameters(GetSqlParameters(@params.ToTuples().ToArray(), insertInto.DvlSqlTypes));
            insertInto.ValuesExpression.SqlParameters = insertInto.Parameters;

            return new SqlInsertDeleteExecutable<int>(this.DvlSqlConnection, ToString,
                GetDvlSqlParameters,
                (command, timeout, token) => command.ExecuteNonQueryAsync(timeout, token ?? default));
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            var commandBuilder = new DvlSqlCommandBuilder(builder);

            this.InsertIntoExpression.Accept(commandBuilder);

            return builder.ToString();
        }

        protected IEnumerable<DvlSqlParameter> GetDvlSqlParameters() => this.InsertIntoExpression.Parameters;
    }

    internal class BaseOutputable<TParam, TResult>(DvlSqlInsertExpression insertIntoExpression, IDvlSqlConnection conn,
        Func<IDataReader, TResult> func) : BaseInsertable<TParam>(insertIntoExpression, conn) where TParam : ITuple
    {
        protected readonly Func<IDataReader, TResult> Reader = func;

        protected new IInsertDeleteExecutable<TResult> Values(params TParam[] @params)
        {
            var insertInto = this.InsertIntoExpression as DvlSqlInsertIntoExpression<TParam>
                             ?? throw new Exception("Can not Convert to Generic insert Expression");
            insertInto.ValuesExpression = ValuesExp(@params);
            insertInto.WithParameters(GetSqlParameters(@params.ToTuples().ToArray(), insertInto.DvlSqlTypes));
            insertInto.ValuesExpression.SqlParameters = insertInto.Parameters;

            return new SqlInsertDeleteExecutable<TResult>(this.DvlSqlConnection, ToString,
                GetDvlSqlParameters,
                (command, timeout, token) =>
                    command.ExecuteReaderAsync(Reader, timeout, cancellationToken: token ?? default));
        }

        protected IInsertDeleteExecutable<TResult> Select(DvlSqlFullSelectExpression fullSelect)
        {
            this.InsertIntoExpression = new DvlSqlInsertIntoSelectExpression(this.InsertIntoExpression.TableName,
                this.InsertIntoExpression.Columns) {SelectExpression = fullSelect,
                OutputExpression = InsertIntoExpression.OutputExpression};

            if (fullSelect.From is DvlSqlValuesExpression valuesExp)
                this.InsertIntoExpression.WithParameters(valuesExp.SqlParameters);

            return new SqlInsertDeleteExecutable<TResult>(this.DvlSqlConnection, ToString,
                GetDvlSqlParameters,
                (command, timeout, token) =>
                    command.ExecuteReaderAsync(Reader, timeout, cancellationToken: token ?? default));
        }
    }

    // ReSharper disable once IdentifierTypo
    internal class SqlInsertable<TParam>(DvlSqlInsertIntoExpression<TParam> insertIntoExpression, IDvlSqlConnection dvlSqlConnection) : BaseInsertable<TParam>(insertIntoExpression, dvlSqlConnection), IInsertable<TParam> where TParam : ITuple
    {
        public IInsertOutputable<TParam, TResult> Output<TResult>(Func<IDataReader, TResult> reader,
            params string[] cols)
        {
            this.SetOutputExpression(cols);
            return new InsertOutputable<TParam, TResult>(InsertIntoExpression, DvlSqlConnection, reader);
        }

        public new IInsertDeleteExecutable<int> Values(params TParam[] @params) => base.Values(@params);
    }

    internal class InsertOutputable<TParam, TResult>(DvlSqlInsertExpression insertIntoExpression, IDvlSqlConnection dvlSqlConnection,
        Func<IDataReader, TResult> reader) : BaseOutputable<TParam, TResult>(insertIntoExpression, dvlSqlConnection, reader),
        IInsertOutputable<TParam, TResult>
        where TParam : ITuple
    {
        public new IInsertDeleteExecutable<TResult> Select(DvlSqlFullSelectExpression fullSelect) =>
            base.Select(fullSelect);

        public new IInsertDeleteExecutable<TResult> Values(params TParam[] @params) => base.Values(@params);
    }

    internal class Insertable<T1, T2>(DvlSqlInsertIntoExpression<(T1, T2)> insertIntoExpression, IDvlSqlConnection dvlSqlConnection) : BaseInsertable<(T1, T2)>(insertIntoExpression, dvlSqlConnection), IInsertable<T1, T2>
    {
        public IInsertable<T1, T2> Output(DvlSqlTableDeclarationExpression intoTable, params string[] cols)
        {
            this.SetOutputExpression(intoTable, cols);
            return this;
        }

        public IInsertOutputable<T1, T2, TResult> Output<TResult>(Func<IDataReader, TResult> reader,
            params string[] cols)
        {
            this.SetOutputExpression(cols);
            return new InsertOutputable<T1, T2, TResult>(InsertIntoExpression, DvlSqlConnection, reader);
        }

        IInsertDeleteExecutable<int> IInsertable<T1, T2>.Values(params (T1 param1, T2 param2)[] @params) =>
            base.Values(@params);
    }

    internal class InsertOutputable<T1, T2, TResult>(DvlSqlInsertExpression insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection,
        Func<IDataReader, TResult> reader) : BaseOutputable<(T1, T2), TResult>(insertIntoExpression, dvlSqlConnection, reader),
        IInsertOutputable<T1, T2, TResult>
    {
        public new IInsertDeleteExecutable<TResult> Select(DvlSqlFullSelectExpression fullSelect) =>
            base.Select(fullSelect);

        public new IInsertDeleteExecutable<TResult> Values(params (T1, T2)[] @params) => base.Values(@params);
    }

    internal class Insertable<T1, T2, T3>(DvlSqlInsertIntoExpression<(T1, T2, T3)> insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection) : BaseInsertable<(T1, T2, T3)>(insertIntoExpression, dvlSqlConnection), IInsertable<T1, T2, T3>
    {
        public IInsertable<T1, T2, T3> Output(DvlSqlTableDeclarationExpression intoTable, params string[] cols)
        {
            this.SetOutputExpression(intoTable, cols);
            return this;
        }


        public IInsertOutputable<T1, T2, T3, TResult> Output<TResult>(Func<IDataReader, TResult> reader,
            params string[] cols)
        {
            this.SetOutputExpression(cols);
            return new InsertOutputable<T1, T2, T3, TResult>(InsertIntoExpression, DvlSqlConnection, reader);
        }

        public new IInsertDeleteExecutable<int> Values(params (T1 param1, T2 param2, T3 param3)[] @params) =>
            base.Values(@params);
    }

    internal class InsertOutputable<T1, T2, T3, TResult>(DvlSqlInsertExpression insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection,
        Func<IDataReader, TResult> reader) : BaseOutputable<(T1, T2, T3), TResult>(insertIntoExpression, dvlSqlConnection, reader),
        IInsertOutputable<T1, T2, T3, TResult>
    {
        public new IInsertDeleteExecutable<TResult> Select(DvlSqlFullSelectExpression fullSelect) =>
            base.Select(fullSelect);

        public new IInsertDeleteExecutable<TResult> Values(params (T1, T2, T3)[] @params) => base.Values(@params);
    }

    internal class Insertable<T1, T2, T3, T4>(DvlSqlInsertIntoExpression<(T1, T2, T3, T4)> insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection) : BaseInsertable<(T1, T2, T3, T4)>(insertIntoExpression, dvlSqlConnection), IInsertable<T1, T2, T3, T4>
    {
        public IInsertable<T1, T2, T3, T4> Output(DvlSqlTableDeclarationExpression intoTable, params string[] cols)
        {
            this.SetOutputExpression(intoTable, cols);
            return this;
        }


        public IInsertOutputable<T1, T2, T3, T4, TResult> Output<TResult>(Func<IDataReader, TResult> reader,
            params string[] cols)
        {
            this.SetOutputExpression(cols);
            return new InsertOutputable<T1, T2, T3, T4, TResult>(InsertIntoExpression, DvlSqlConnection, reader);
        }

        public new IInsertDeleteExecutable<int> Values(params (T1 param1, T2 param2, T3 param3, T4 param4)[] @params) =>
            base.Values(@params);
    }

    internal class InsertOutputable<T1, T2, T3, T4, TResult>(DvlSqlInsertExpression insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection,
        Func<IDataReader, TResult> reader) : BaseOutputable<(T1, T2, T3, T4), TResult>(insertIntoExpression, dvlSqlConnection, reader),
        IInsertOutputable<T1, T2, T3, T4, TResult>
    {
        public new IInsertDeleteExecutable<TResult> Select(DvlSqlFullSelectExpression fullSelect) =>
            base.Select(fullSelect);

        public new IInsertDeleteExecutable<TResult> Values(params (T1, T2, T3, T4)[] @params) => base.Values(@params);
    }

    internal class Insertable<T1, T2, T3, T4, T5>(DvlSqlInsertIntoExpression<(T1, T2, T3, T4, T5)> insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection) : BaseInsertable<(T1, T2, T3, T4, T5)>(insertIntoExpression, dvlSqlConnection),
        IInsertable<T1, T2, T3, T4, T5>
    {
        public IInsertable<T1, T2, T3, T4, T5> Output(DvlSqlTableDeclarationExpression intoTable, params string[] cols)
        {
            this.SetOutputExpression(intoTable, cols);
            return this;
        }

        public IInsertOutputable<T1, T2, T3, T4, T5, TResult> Output<TResult>(Func<IDataReader, TResult> reader,
            params string[] cols)
        {
            this.SetOutputExpression(cols);
            return new InsertOutputable<T1, T2, T3, T4, T5, TResult>(InsertIntoExpression, DvlSqlConnection, reader);
        }

        public new IInsertDeleteExecutable<int> Values(
            params (T1 param1, T2 param2, T3 param3, T4 param4, T5 param5)[] @params) => base.Values(@params);
    }

    internal class InsertOutputable<T1, T2, T3, T4, T5, TResult>(DvlSqlInsertExpression insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection,
        Func<IDataReader, TResult> reader) : BaseOutputable<(T1, T2, T3, T4, T5), TResult>(insertIntoExpression, dvlSqlConnection, reader),
        IInsertOutputable<T1, T2, T3, T4, T5, TResult>
    {
        public new IInsertDeleteExecutable<TResult> Select(DvlSqlFullSelectExpression fullSelect) =>
            base.Select(fullSelect);

        public new IInsertDeleteExecutable<TResult> Values(params (T1, T2, T3, T4, T5)[] @params) =>
            base.Values(@params);
    }

    internal class Insertable<T1, T2, T3, T4, T5, T6>(DvlSqlInsertIntoExpression<(T1, T2, T3, T4, T5, T6)> insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection) : BaseInsertable<(T1, T2, T3, T4, T5, T6)>(insertIntoExpression, dvlSqlConnection),
        IInsertable<T1, T2, T3, T4, T5, T6>
    {
        public IInsertable<T1, T2, T3, T4, T5, T6> Output(DvlSqlTableDeclarationExpression intoTable,
            params string[] cols)
        {
            this.SetOutputExpression(intoTable, cols);
            return this;
        }

        public IInsertOutputable<T1, T2, T3, T4, T5, T6, TResult> Output<TResult>(Func<IDataReader, TResult> reader,
            params string[] cols)
        {
            this.SetOutputExpression(cols);
            return new InsertOutputable<T1, T2, T3, T4, T5, T6, TResult>(InsertIntoExpression, DvlSqlConnection, reader);
        }

        public new IInsertDeleteExecutable<int> Values(
            params (T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6)[] @params) =>
            base.Values(@params);
    }

    internal class InsertOutputable<T1, T2, T3, T4, T5, T6, TResult>(DvlSqlInsertExpression insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection,
        Func<IDataReader, TResult> reader) :
        BaseOutputable<(T1, T2, T3, T4, T5, T6), TResult>(insertIntoExpression, dvlSqlConnection, reader), IInsertOutputable<T1, T2, T3, T4, T5, T6, TResult>
    {
        public new IInsertDeleteExecutable<TResult> Select(DvlSqlFullSelectExpression fullSelect) =>
            base.Select(fullSelect);

        public new IInsertDeleteExecutable<TResult> Values(params (T1, T2, T3, T4, T5, T6)[] @params) =>
            base.Values(@params);
    }

    internal class Insertable<T1, T2, T3, T4, T5, T6, T7>(DvlSqlInsertIntoExpression<(T1, T2, T3, T4, T5, T6, T7)> insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection) : BaseInsertable<(T1, T2, T3, T4, T5, T6, T7)>(insertIntoExpression, dvlSqlConnection),
        IInsertable<T1, T2, T3, T4, T5, T6, T7>
    {
        public IInsertable<T1, T2, T3, T4, T5, T6, T7> Output(DvlSqlTableDeclarationExpression intoTable,
            params string[] cols)
        {
            this.SetOutputExpression(intoTable, cols);
            return this;
        }

        public IInsertOutputable<T1, T2, T3, T4, T5, T6, T7, TResult> Output<TResult>(Func<IDataReader, TResult> reader,
            params string[] cols)
        {
            this.SetOutputExpression(cols);
            return new InsertOutputable<T1, T2, T3, T4, T5, T6, T7, TResult>(InsertIntoExpression, DvlSqlConnection,
                reader);
        }

        public new IInsertDeleteExecutable<int> Values(
            params (T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7)[] @params) =>
            base.Values(@params);
    }

    internal class InsertOutputable<T1, T2, T3, T4, T5, T6, T7, TResult>(DvlSqlInsertExpression insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection,
        Func<IDataReader, TResult> reader) :
        BaseOutputable<(T1, T2, T3, T4, T5, T6, T7), TResult>(insertIntoExpression, dvlSqlConnection, reader), IInsertOutputable<T1, T2, T3, T4, T5, T6, T7, TResult>
    {
        public new IInsertDeleteExecutable<TResult> Select(DvlSqlFullSelectExpression fullSelect) =>
            base.Select(fullSelect);

        public new IInsertDeleteExecutable<TResult> Values(params (T1, T2, T3, T4, T5, T6, T7)[] @params) =>
            base.Values(@params);
    }

    internal class Insertable<T1, T2, T3, T4, T5, T6, T7, T8>(DvlSqlInsertIntoExpression<(T1, T2, T3, T4, T5, T6, T7, T8)> insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection) : BaseInsertable<(T1, T2, T3, T4, T5, T6, T7, T8)>(insertIntoExpression, dvlSqlConnection),
        IInsertable<T1, T2, T3, T4, T5, T6, T7, T8>
    {
        public IInsertable<T1, T2, T3, T4, T5, T6, T7, T8> Output(DvlSqlTableDeclarationExpression intoTable,
            params string[] cols)
        {
            this.SetOutputExpression(intoTable, cols);
            return this;
        }

        public IInsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, TResult> Output<TResult>(
            Func<IDataReader, TResult> reader, params string[] cols)
        {
            this.SetOutputExpression(cols);
            return new InsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(InsertIntoExpression, DvlSqlConnection,
                reader);
        }

        public new IInsertDeleteExecutable<int> Values(
            params (T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8)[]
                @params) => base.Values(@params);
    }

    internal class InsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(DvlSqlInsertExpression insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection,
        Func<IDataReader, TResult> reader) :
        BaseOutputable<(T1, T2, T3, T4, T5, T6, T7, T8), TResult>(insertIntoExpression, dvlSqlConnection, reader),
        IInsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, TResult>
    {
        public new IInsertDeleteExecutable<TResult> Select(DvlSqlFullSelectExpression fullSelect) =>
            base.Select(fullSelect);

        public new IInsertDeleteExecutable<TResult> Values(params (T1, T2, T3, T4, T5, T6, T7, T8)[] @params) =>
            base.Values(@params);
    }

    internal class Insertable<T1, T2, T3, T4, T5, T6, T7, T8, T9>(DvlSqlInsertIntoExpression<(T1, T2, T3, T4, T5, T6, T7, T8, T9)> insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection) :
        BaseInsertable<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>(insertIntoExpression, dvlSqlConnection), IInsertable<T1, T2, T3, T4, T5, T6, T7, T8, T9>
    {
        public IInsertable<T1, T2, T3, T4, T5, T6, T7, T8, T9> Output(DvlSqlTableDeclarationExpression intoTable,
            params string[] cols)
        {
            this.SetOutputExpression(intoTable, cols);
            return this;
        }

        public IInsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> Output<TResult>(
            Func<IDataReader, TResult> reader, params string[] cols)
        {
            this.SetOutputExpression(cols);
            return new InsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(InsertIntoExpression, DvlSqlConnection,
                reader);
        }

        public new IInsertDeleteExecutable<int> Values(
            params (T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9)[]
                @params) => base.Values(@params);
    }

    internal class InsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(DvlSqlInsertExpression insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection,
        Func<IDataReader, TResult> reader) :
        BaseOutputable<(T1, T2, T3, T4, T5, T6, T7, T8, T9), TResult>(insertIntoExpression, dvlSqlConnection, reader),
        IInsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>
    {
        public new IInsertDeleteExecutable<TResult> Select(DvlSqlFullSelectExpression fullSelect) =>
            base.Select(fullSelect);

        public new IInsertDeleteExecutable<TResult> Values(params (T1, T2, T3, T4, T5, T6, T7, T8, T9)[] @params) =>
            base.Values(@params);
    }

    internal class Insertable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(DvlSqlInsertIntoExpression<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)> insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection) :
        BaseInsertable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>(insertIntoExpression, dvlSqlConnection), IInsertable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
    {
        public IInsertable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Output(DvlSqlTableDeclarationExpression intoTable,
            params string[] cols)
        {
            this.SetOutputExpression(intoTable, cols);
            return this;
        }

        public IInsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> Output<TResult>(
            Func<IDataReader, TResult> reader, params string[] cols)
        {
            this.SetOutputExpression(cols);
            return new InsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(InsertIntoExpression,
                DvlSqlConnection, reader);
        }

        public new IInsertDeleteExecutable<int> Values(
            params (T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9,
                T10 param10)[] @params) => base.Values(@params);
    }

    internal class InsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(DvlSqlInsertExpression insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection,
        Func<IDataReader, TResult> reader) :
        BaseOutputable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10), TResult>(insertIntoExpression, dvlSqlConnection, reader),
        IInsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>
    {
        public new IInsertDeleteExecutable<TResult> Select(DvlSqlFullSelectExpression fullSelect) =>
            base.Select(fullSelect);

        public new IInsertDeleteExecutable<TResult>
            Values(params (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)[] @params) => base.Values(@params);
    }

    internal class Insertable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(DvlSqlInsertIntoExpression<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)> insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection) :
        BaseInsertable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>(insertIntoExpression, dvlSqlConnection),
        IInsertable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
    {
        public IInsertable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Output(
            DvlSqlTableDeclarationExpression intoTable, params string[] cols)
        {
            this.SetOutputExpression(intoTable, cols);
            return this;
        }

        public IInsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> Output<TResult>(
            Func<IDataReader, TResult> reader, params string[] cols)
        {
            this.SetOutputExpression(cols);
            return new InsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(InsertIntoExpression,
                DvlSqlConnection, reader);
        }

        public new IInsertDeleteExecutable<int> Values(
            params (T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9,
                T10 param10, T11 param11)[] @params) => base.Values(@params);
    }

    internal class InsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(
        DvlSqlInsertExpression insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection,
        Func<IDataReader, TResult> reader) :
        BaseOutputable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11), TResult>(insertIntoExpression, dvlSqlConnection, reader),
        IInsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>
    {
        public new IInsertDeleteExecutable<TResult> Select(DvlSqlFullSelectExpression fullSelect) =>
            base.Select(fullSelect);

        public new IInsertDeleteExecutable<TResult> Values(
            params (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)[] @params) => base.Values(@params);
    }

    internal class Insertable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
        DvlSqlInsertIntoExpression<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)> insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection) :
        BaseInsertable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>(insertIntoExpression, dvlSqlConnection),
        IInsertable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
    {
        public IInsertable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Output(
            DvlSqlTableDeclarationExpression intoTable, params string[] cols)
        {
            this.SetOutputExpression(intoTable, cols);
            return this;
        }

        public IInsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> Output<TResult>(
            Func<IDataReader, TResult> reader, params string[] cols)
        {
            this.SetOutputExpression(cols);
            return new InsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(InsertIntoExpression,
                DvlSqlConnection, reader);
        }

        public new IInsertDeleteExecutable<int> Values(
            params (T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9,
                T10 param10, T11 param11, T12 param12)[] @params) => base.Values(@params);
    }

    internal class InsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(
        DvlSqlInsertExpression insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection,
        Func<IDataReader, TResult> reader) :
        BaseOutputable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12), TResult>(insertIntoExpression, dvlSqlConnection, reader),
        IInsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>
    {
        public new IInsertDeleteExecutable<TResult> Select(DvlSqlFullSelectExpression fullSelect) =>
            base.Select(fullSelect);

        public new IInsertDeleteExecutable<TResult> Values(
            params (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)[] @params) => base.Values(@params);
    }

    internal class Insertable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
        DvlSqlInsertIntoExpression<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)> insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection) :
        BaseInsertable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>(insertIntoExpression, dvlSqlConnection),
        IInsertable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
    {
        public IInsertable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Output(
            DvlSqlTableDeclarationExpression intoTable, params string[] cols)
        {
            this.SetOutputExpression(intoTable, cols);
            return this;
        }

        public IInsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> Output<TResult>(
            Func<IDataReader, TResult> reader, params string[] cols)
        {
            this.SetOutputExpression(cols);
            return new InsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(
                InsertIntoExpression, DvlSqlConnection, reader);
        }

        public new IInsertDeleteExecutable<int> Values(
            params (T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9,
                T10 param10, T11 param11, T12 param12, T13 param13)[] @params) => base.Values(@params);
    }

    internal class InsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(
        DvlSqlInsertExpression insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection,
        Func<IDataReader, TResult> reader) :
        BaseOutputable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13), TResult>(insertIntoExpression, dvlSqlConnection, reader),
        IInsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>
    {
        public new IInsertDeleteExecutable<TResult> Select(DvlSqlFullSelectExpression fullSelect) =>
            base.Select(fullSelect);

        public new IInsertDeleteExecutable<TResult> Values(
            params (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)[] @params) => base.Values(@params);
    }

    internal class Insertable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
        DvlSqlInsertIntoExpression<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)> insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection) :
        BaseInsertable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>(insertIntoExpression, dvlSqlConnection),
        IInsertable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
    {
        public IInsertable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Output(
            DvlSqlTableDeclarationExpression intoTable, params string[] cols)
        {
            this.SetOutputExpression(intoTable, cols);
            return this;
        }

        public IInsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> Output<TResult>(
            Func<IDataReader, TResult> reader, params string[] cols)
        {
            this.SetOutputExpression(cols);
            return new InsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(
                InsertIntoExpression, DvlSqlConnection, reader);
        }

        public new IInsertDeleteExecutable<int> Values(
            params (T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9,
                T10 param10, T11 param11, T12 param12, T13 param13, T14 param14)[] @params) => base.Values(@params);
    }

    internal class InsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(
        DvlSqlInsertExpression insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection,
        Func<IDataReader, TResult> reader) :
        BaseOutputable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14), TResult>(insertIntoExpression, dvlSqlConnection, reader),
        IInsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>
    {
        public new IInsertDeleteExecutable<TResult> Select(DvlSqlFullSelectExpression fullSelect) =>
            base.Select(fullSelect);

        public new IInsertDeleteExecutable<TResult> Values(
            params (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)[] @params) => base.Values(@params);
    }

    internal class Insertable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
        DvlSqlInsertIntoExpression<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>
                insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection) :
        BaseInsertable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>(insertIntoExpression, dvlSqlConnection),
        IInsertable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
    {
        public IInsertable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Output(
            DvlSqlTableDeclarationExpression intoTable, params string[] cols)
        {
            this.SetOutputExpression(intoTable, cols);
            return this;
        }

        public IInsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>
            Output<TResult>(Func<IDataReader, TResult> reader, params string[] cols)
        {
            this.SetOutputExpression(cols);
            return new InsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(
                InsertIntoExpression, DvlSqlConnection, reader);
        }

        public new IInsertDeleteExecutable<int> Values(
            params (T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9,
                T10 param10, T11 param11, T12 param12, T13 param13, T14 param14, T15 param15)[] @params) =>
            base.Values(@params);
    }

    internal class InsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(
        DvlSqlInsertExpression
                insertIntoExpression, IDvlSqlConnection dvlSqlConnection,
        Func<IDataReader, TResult> reader) :
        BaseOutputable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15), TResult>(insertIntoExpression, dvlSqlConnection, reader),
        IInsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>
    {
        public new IInsertDeleteExecutable<TResult> Select(DvlSqlFullSelectExpression fullSelect) =>
            base.Select(fullSelect);

        public new IInsertDeleteExecutable<TResult> Values(
            params (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)[] @params) =>
            base.Values(@params);
    }

    internal class Insertable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(
        DvlSqlInsertIntoExpression<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>
                insertIntoExpression,
        IDvlSqlConnection dvlSqlConnection) :
        BaseInsertable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>(insertIntoExpression, dvlSqlConnection),
        IInsertable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>
    {
        public IInsertable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Output(
            DvlSqlTableDeclarationExpression intoTable, params string[] cols)
        {
            this.SetOutputExpression(intoTable, cols);
            return this;
        }

        public IInsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>
            Output<TResult>(Func<IDataReader, TResult> reader, params string[] cols)
        {
            this.SetOutputExpression(cols);
            return new InsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(
                InsertIntoExpression, DvlSqlConnection, reader);
        }

        public new IInsertDeleteExecutable<int> Values(
            params (T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8, T9 param9,
                T10 param10, T11 param11, T12 param12, T13 param13, T14 param14, T15 param15, T16 param16)[] @params) =>
            base.Values(@params);
    }

    internal class InsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(
        DvlSqlInsertExpression
                insertIntoExpression, IDvlSqlConnection dvlSqlConnection,
        Func<IDataReader, TResult> reader) :
        BaseOutputable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16), TResult>(insertIntoExpression, dvlSqlConnection, reader),
        IInsertOutputable<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>
    {
        public new IInsertDeleteExecutable<TResult> Select(DvlSqlFullSelectExpression fullSelect) =>
            base.Select(fullSelect);

        public new IInsertDeleteExecutable<TResult> Values(
            params (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)[] @params) =>
            base.Values(@params);
    }

    // ReSharper disable once IdentifierTypo
    internal class SqlInsertable (DvlSqlInsertIntoSelectExpression insertExpression, IDvlSqlConnection conn): IInsertable
    {
        private readonly DvlSqlInsertIntoSelectExpression _insertWithSelectExpression = insertExpression;
        private readonly IDvlSqlConnection _dvlSqlConnection = conn;

        public IInsertDeleteExecutable<int> SelectStatement(DvlSqlFullSelectExpression selectExpression,
            params DvlSqlParameter[] @params)
        {
            this._insertWithSelectExpression.SelectExpression = selectExpression;
            this._insertWithSelectExpression.Parameters = [.. @params];

            return new SqlInsertDeleteExecutable<int>(this._dvlSqlConnection, ToString, GetDvlSqlParameters,
                (command, timeout, token) => command.ExecuteNonQueryAsync(timeout, token ?? default));
        }

        private List<DvlSqlParameter> GetDvlSqlParameters() => this._insertWithSelectExpression.Parameters;

        public override string ToString()
        {
            var builder = new StringBuilder();
            var commandBuilder = new DvlSqlCommandBuilder(builder);

            this._insertWithSelectExpression.Accept(commandBuilder);

            return builder.ToString();
        }
    }
}