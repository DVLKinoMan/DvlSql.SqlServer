﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using static System.Exts.Extensions;

namespace DvlSql.SqlServer
{
    internal static class Exts
    {
        internal static Func<IDataReader, TResult?> RecordReaderFunc<TResult>() =>
          reader =>
          {
              if (typeof(TResult).IsClass && typeof(TResult).Namespace != "System")
                  return GetObjectOfType<TResult>(reader);

              return reader[0] != DBNull.Value ? (TResult?)reader[0] : default;
          };

        internal static T GetObjectOfType<T>(this IDataReader r)
        {
            var instance = Activator.CreateInstance<T>();
            foreach (var innerProp in typeof(T).GetProperties())
                if (innerProp.PropertyType.Namespace == "System" &&
                    !innerProp.PropertyType.IsGenericType(typeof(ICollection<>)) &&
                    r[innerProp.Name] != DBNull.Value)
                    innerProp.SetValue(instance, r[innerProp.Name]);

            return instance;
        }

        internal static IEnumerable<SqlParameter> ToSqlParameters(this IEnumerable<DvlSqlParameter> parameters) =>
            parameters.Select(param => param.ToSqlParameter());

        internal static SqlParameter ToSqlParameter(this DvlSqlParameter parameter)
        {
            var isOuput = parameter is DvlSqlOutputParameter;
            var param = new SqlParameter(parameter.Name.GetStringAfter("."), parameter.DvlSqlType.SqlDbType)
            {
                Direction = isOuput ? ParameterDirection.Output : ParameterDirection.Input
            };

            if (isOuput)
                param.Value = DBNull.Value;
            else if (parameter.DvlSqlType.GetType().GetGenericTypeDefinition() == typeof(DvlSqlType<>))
            {
                var prop = parameter.DvlSqlType.GetType().GetProperty("Value") ?? throw new MissingMemberException("Value");
                param.Value = prop.GetValue(parameter.DvlSqlType) ?? DBNull.Value;
            }

            if (parameter.DvlSqlType.Size != null)
                param.Size = parameter.DvlSqlType.Size.Value;

            if (parameter.DvlSqlType.Precision != null)
                param.Precision = parameter.DvlSqlType.Precision.Value;

            if (parameter.DvlSqlType.Scale != null)
                param.Scale = parameter.DvlSqlType.Scale.Value;

            return param;
        }
    }
}
