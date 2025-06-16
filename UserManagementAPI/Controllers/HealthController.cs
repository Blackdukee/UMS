using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UserManagementAPI.Controllers
{
    [Route("api/v1/ums/health")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        /// <summary>
        /// Health check endpoint to verify the API is running.
        /// </summary>
        /// <returns>Returns a simple status message.</returns>
        [HttpGet]
        public IActionResult GetHealthStatus()
        {
            return Ok(new { status = "API is running", timestamp = DateTime.UtcNow });
        }
    }
    
}
