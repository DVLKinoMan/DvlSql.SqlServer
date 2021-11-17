using System;
using System.Collections.Generic;

namespace DvlSql.SqlServer.TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString =
                  "Data Source = DESKTOP-D5ADL3B\\MSSQLSERVER01; Initial Catalog = CasbinTest; integrated security = true";

            var dvl_sql = new DvlSqlMs(connectionString);

            //Select ids from table ordered by date
            List<int> ids = dvl_sql.From("casbin_rule")
                                    .Select("id", "v0", "v1")
                                    .ToListAsync(r => (int)r["id"])
                                    .Result;
        }
    }
}
