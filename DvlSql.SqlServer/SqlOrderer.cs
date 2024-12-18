﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using static DvlSql.Extensions.DataReader;

namespace DvlSql.SqlServer;

internal class SqlOrderer(IDvlSqlConnection connection, SqlSelector selector) : IOrderer
{
    private readonly SqlSelector _selector = selector;
    private readonly IDvlSqlConnection _connection = connection;

    public IOrderExecutable OrderBy(params string[] fields) 
        => this._selector.OrderBy(this, fields);

    public IOrderExecutable OrderByDescending(params string[] fields) 
        => this._selector.OrderByDescending(this, fields);

    public ISelectExecutable Skip(
        int offsetRows, 
        int? fetchNextRows = null) 
        => this._selector.Skip(this, 
            offsetRows, 
            fetchNextRows
            );

    public async Task<List<TResult?>> ToListAsync<TResult>(
        Func<IDataReader, TResult?> selectorFunc,
        int? timeout = default,
        CommandBehavior behavior = CommandBehavior.Default, 
        CancellationToken cancellationToken = default) 
        => await this._connection.ConnectAsync(
            dvlCommand => dvlCommand.ExecuteReaderAsync(
                AsList(selectorFunc), 
                timeout, 
                behavior, 
                cancellationToken),
            this._selector.ToString(),
            parameters: this._selector.GetDvlSqlParameters()?.ToArray());

    public async Task<List<TResult?>> ToListAsync<TResult>(
        int? timeout = default,
        CommandBehavior behavior = CommandBehavior.Default, 
        CancellationToken cancellationToken = default)
        => await this._connection.ConnectAsync(
            dvlCommand => dvlCommand.ExecuteReaderAsync(
                AsList(RecordReaderFunc<TResult>()), 
                timeout, 
                behavior, 
                cancellationToken),
            this._selector.ToString(),
            parameters: this._selector.GetDvlSqlParameters()?.ToArray());

    public async Task<Dictionary<TKey, List<TValue?>>> ToDictionaryAsync<TKey, TValue>(
        Func<IDataReader, TKey> keySelector,
        Func<IDataReader, TValue?> valueSelector,
        int? timeout = default,
        CommandBehavior behavior = CommandBehavior.Default, 
        CancellationToken cancellationToken = default)
        => await this._connection.ConnectAsync(
            dvlCommand => dvlCommand.ExecuteReaderAsync(
                AsDictionary(keySelector, valueSelector), 
                timeout, 
                behavior,
                cancellationToken),
            this._selector.ToString(),
            parameters: this._selector.GetDvlSqlParameters()?.ToArray());

    public async Task<TResult> FirstAsync<TResult>(
        int? timeout = default,
        CancellationToken cancellationToken = default)
        => await FirstAsync<TResult>(
            RecordReaderFunc<TResult>(() => throw new ArgumentException(null, nameof(TResult)))!,
            timeout, 
            cancellationToken);

    public async Task<TResult> FirstAsync<TResult>(
        Func<IDataReader, TResult> readerFunc, 
        int? timeout = default,
        CancellationToken cancellationToken = default)
        => await this._connection.ConnectAsync(
            dvlCommand =>
                dvlCommand.ExecuteReaderAsync(
                    First(readerFunc), 
                    timeout, 
                    cancellationToken: cancellationToken),
            this._selector.WithSelectTop(1).ToString(),
            parameters: this._selector.GetDvlSqlParameters()?.ToArray());

    public async Task<TResult?> FirstOrDefaultAsync<TResult>(
        int? timeout = default,
        CancellationToken cancellationToken = default)
        => await FirstOrDefaultAsync(
            RecordReaderFunc<TResult>(), 
            timeout, 
            cancellationToken);

    public async Task<TResult?> FirstOrDefaultAsync<TResult>(
        Func<IDataReader, TResult?> readerFunc,
        int? timeout = default, 
        CancellationToken cancellationToken = default) 
        => await this._connection.ConnectAsync(
            dvlCommand =>
                dvlCommand.ExecuteReaderAsync(
                    FirstOrDefault(readerFunc), 
                    timeout, 
                    cancellationToken: cancellationToken),
            this._selector.WithSelectTop(1).ToString(),
            parameters: this._selector.GetDvlSqlParameters()?.ToArray());

    public async Task<bool> AnyAsync(
        int? timeout = default,
        CancellationToken cancellationToken = default) 
        => await this._connection.ConnectAsync(
            dvlCommand =>
                dvlCommand.ExecuteReaderAsync(
                    r => r.Read(), 
                    timeout,
                    cancellationToken: cancellationToken),
            this._selector.WithSelectTop(1).ToString(),
            parameters: this._selector.GetDvlSqlParameters()?.ToArray());

    public async Task<bool> AllAsync(
        int? timeout = default,
        CancellationToken cancellationToken = default)
    {
        _selector.NotExpOnFullSelects();
        return !(await AnyAsync(timeout, cancellationToken));
    }

    public async Task<TResult>
        SingleAsync<TResult>(
        int? timeout = null, 
        CancellationToken cancellationToken = default)
        => await SingleAsync<TResult>(
            RecordReaderFunc<TResult>(() => throw new ArgumentException(null, nameof(TResult)))!,
            timeout, 
            cancellationToken);

    public async Task<TResult> SingleAsync<TResult>(
        Func<IDataReader, TResult> readerFunc, 
        int? timeout = null,
        CancellationToken cancellationToken = default) 
        => await this._connection.ConnectAsync(
            dvlCommand =>
                dvlCommand.ExecuteReaderAsync(
                    Single(readerFunc), 
                    timeout, 
                    cancellationToken: cancellationToken),
            this._selector.ToString(),
            parameters: this._selector.GetDvlSqlParameters()?.ToArray());

    public async Task<TResult?> SingleOrDefaultAsync<TResult>(
        int? timeout = null,
        CancellationToken cancellationToken = default) 
        => await SingleOrDefaultAsync(
            RecordReaderFunc<TResult>(() => throw new ArgumentException(null, nameof(TResult))),
            timeout, 
            cancellationToken);

    public async Task<TResult?> SingleOrDefaultAsync<TResult>(
        Func<IDataReader, TResult> readerFunc,
        int? timeout = null, 
        CancellationToken cancellationToken = default)
        => await this._connection.ConnectAsync(
            dvlCommand =>
                dvlCommand.ExecuteReaderAsync(SingleOrDefault(readerFunc), 
                    timeout,
                    cancellationToken: cancellationToken),
            this._selector.ToString(),
            parameters: this._selector.GetDvlSqlParameters()?.ToArray());

    public override string ToString() => this._selector.ToString();

    public IFromable Union() => this._selector.Union();
    public IFromable UnionAll() => this._selector.UnionAll();
    
}