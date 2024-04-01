using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using static System.Exts.Extensions;

namespace DvlSql.SqlServer
{
    internal static class Exts
    {
        internal static Func<IDataReader, TResult?> RecordReaderFunc<TResult>(Func<TResult>? defaultFunc = null) =>
          reader =>
          {
              if (typeof(TResult).IsClass && typeof(TResult).Namespace != "System")
                  return GetObjectOfType(reader, defaultFunc);

              return reader.FieldCount > 0 && reader[0] != DBNull.Value ? (TResult?)reader[0] : defaultFunc == null ? default : defaultFunc();
          };

        internal static T? GetObjectOfType<T>(this IDataReader r, Func<T>? defaultFunc = null)
        {
            var instance = Activator.CreateInstance<T>();
            bool anyPropertySet = false;
            var type = typeof(T);
            for (int i = 0; i < r.FieldCount; i++)
            {
                var fieldName = r.GetName(i);
                var prop = type.GetProperty(fieldName);
                if (prop != null &&
                    prop.PropertyType.Namespace == "System" &&
                    //!prop.PropertyType.IsGenericType(typeof(ICollection<>)) &&
                    r[prop.Name] != DBNull.Value)
                {
                    anyPropertySet = true;
                    prop.SetValue(instance, r[prop.Name]);
                }
            }

            return anyPropertySet ? instance : defaultFunc == null ? default : defaultFunc();
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
