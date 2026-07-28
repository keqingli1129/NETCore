using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace NZWalks.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SudentsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAllStudents()
        {
            return Ok(new[] { "Student1", "Student2" });
        }
    }
}
