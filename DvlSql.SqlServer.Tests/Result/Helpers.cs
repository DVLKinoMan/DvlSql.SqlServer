﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

using Moq;

namespace DvlSql.SqlServer.Result
{
    public static class Helpers
    {
        public static Mock<IDataReader> CreateDataReaderMock<T>(IList<T> list)
        {
            var readerMoq = new Mock<IDataReader>();
            int index = -1;

            readerMoq.Setup(reader => reader.Read())
                .Callback(() => { index++; })
                .Returns(() => index < list.Count);

            if (typeof(T).Namespace != "System")
            {
                int ind = 0;
                foreach (var prop in typeof(T).GetProperties())
                    if (prop.PropertyType.Namespace == "System")
                    {
                        readerMoq.Setup(reader => reader.GetName(ind))
                            .Returns(() => prop.Name);
                        ind++;
                    }
                readerMoq.Setup(reader => reader.FieldCount)
                   .Returns(ind);
            }
            else
            {
                readerMoq.Setup(reader => reader.FieldCount)
                    .Returns(1);//>0 value
                readerMoq.Setup(reader => reader[0])
                    .Returns(() => list[index]!);
            }

            return readerMoq;
        }

        /// <summary>
        /// Creates SqlCommandMock with ExecuteReaderAsync Setup
        /// </summary>
        /// <param name="readerMoq"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Mock<IDvlSqlCommand> CreateSqlCommandMock<T>(Mock<IDataReader> readerMoq)
        {
            var commandMoq = new Mock<IDvlSqlCommand>();

            commandMoq.Setup(com => com.ExecuteReaderAsync(It.IsAny<Func<IDataReader, T>>(),
                    It.IsAny<int?>(),
                    It.IsAny<CommandBehavior>(), It.IsAny<CancellationToken>()))
                .Returns((Func<IDataReader, T> func2, int? timeout,
                        CommandBehavior behavior, CancellationToken cancellationToken) =>
                    Task.FromResult(func2(readerMoq.Object)));

            return commandMoq;
        }

        /// <summary>
        /// Creates SqlCommandMock with ExecuteNonQueryAsync Setup
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static Mock<IDvlSqlCommand> CreateSqlCommandMock(int result)
        {
            var commandMoq = new Mock<IDvlSqlCommand>();

            commandMoq.Setup(com => com.ExecuteNonQueryAsync(
                    It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                    Task.FromResult(result));

            return commandMoq;
        }

        public static Mock<IDvlSqlConnection> CreateConnectionMock<T>(Mock<IDvlSqlCommand> commandMoq, CommandType commandType = CommandType.Text)
        {
            var moq = new Mock<IDvlSqlConnection>();

            moq.Setup(m => m.ConnectAsync(It.IsAny<Func<IDvlSqlCommand, Task<T>>>(),
                    It.IsAny<string>(), It.Is<CommandType>(com => com == commandType),
                    It.IsAny<DvlSqlParameter[]>()))
                .Returns((Func<IDvlSqlCommand, Task<T>> func, string sqlString,
                    CommandType type, DvlSqlParameter[] parameters) => func(commandMoq.Object));

            return moq;
        }
    }
}