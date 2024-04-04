using Microsoft.AspNetCore.Mvc;
using static DvlSql.ExpressionHelpers;

namespace DvlSql.SqlServer.Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FilmsController : ControllerBase
    {
        private readonly ILogger<FilmsController> _logger;
        private readonly IDvlSql _dvlSql;

        public FilmsController(ILogger<FilmsController> logger, IDvlSql dvlSql)
        {
            _logger = logger;
            _dvlSql = dvlSql;
        }

        [HttpGet(Name = "GetCount")]
        public async Task<int> GetCount()
        {
            var count = await _dvlSql.From("Films")
                .Select(CountExp())
                .FirstAsync<int>();

            return count;
        }
    }
}
