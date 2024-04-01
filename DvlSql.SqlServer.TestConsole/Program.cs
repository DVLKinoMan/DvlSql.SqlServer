using System;
using System.Collections.Generic;
using System.Linq;
using DvlSql.Extensions;
using static DvlSql.Extensions.ExpressionHelpers;

namespace DvlSql.SqlServer.TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            ArgumentNullException.ThrowIfNull(args);

            string connectionString =
                  "Server=LAPTOP-DEUOP46M\\LOCALHOST; Database=IMDBDatabase; Integrated Security=True;MultipleActiveResultSets=True; Encrypt=False;";

            var dvl_sql = new DvlSqlMs(connectionString);

            //Select ids from table ordered by date
            var films = dvl_sql.From("Films")
                                    .Select()
                                    .ToListAsync<Film>()
                                    .Result;
        }

        public class Film
        {
            public string ImdbpageUrl { get; set; }
            public int? DurationInMinutes { get; set; }
            public string Description { get; set; }
            public string Name { get; set; }
            public string TvDescription { get; set; }
            public string ImdbTitle { get; set; }
            public string AwardsInformationString { get; set; }
            public decimal? Imdbrating { get; set; }
            public int? ImdbuserRatingsCount { get; set; }
            public DateTime? ReleaseDate { get; set; }
            public string Tagline { get; set; }
        }
    }
}
