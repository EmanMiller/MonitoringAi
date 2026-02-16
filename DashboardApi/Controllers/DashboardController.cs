using DashboardApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using DashboardApi.Models;
using Microsoft.Extensions.Configuration; // Add this
using System; // Add this for Exception

namespace DashboardApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _dashboardService;
        private readonly ConfluenceService _confluenceService;
        private readonly IConfiguration _configuration; // Add this

        public DashboardController(
            DashboardService dashboardService, 
            ConfluenceService confluenceService, 
            IConfiguration configuration) // Add this
        {
            _dashboardService = dashboardService;
            _confluenceService = confluenceService;
            _configuration = configuration; // Add this
        }

        [HttpPost]
        public async Task<IActionResult> CreateDashboard([FromBody] DashboardCreationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.DashboardName) || 
                    string.IsNullOrEmpty(request.SourceCategory) || 
                    string.IsNullOrEmpty(request.ConfluencePageId) || 
                    string.IsNullOrEmpty(request.ProjectName))
                {
                    return BadRequest("Request is missing required parameters.");
                }

                var dashboardUrl = await _dashboardService.CreateDashboardAsync(request.DashboardName, request.SourceCategory);
                await _confluenceService.UpdatePageAsync(request.ConfluencePageId, dashboardUrl, request.DashboardName, request.ProjectName);
                return Ok(new { dashboardUrl });
            }
            catch (System.Exception)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while creating the dashboard and updating Confluence.");
            }
        }

        [HttpPost("wizard")]
        public async Task<IActionResult> CreateFromWizard([FromBody] DashboardWizardRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.DashboardTitle))
            {
                return BadRequest("Request is missing required parameters.");
            }

            try
            {
                var dashboardUrl = await _dashboardService.CreateDashboardFromWizardAsync(request);
                
                var confluencePageId = _configuration["Confluence:PageId"];
                if (string.IsNullOrEmpty(confluencePageId))
                {
                    // Log this configuration error
                    return StatusCode(500, "Confluence PageId is not configured.");
                }

                var projectName = "Project From Wizard"; // Placeholder project name

                await _confluenceService.UpdatePageAsync(confluencePageId, dashboardUrl, request.DashboardTitle, projectName);

                return Ok(new { dashboardUrl });
            }
            catch (Exception ex)
            {
                // Log the exception (e.g., using ILogger)
                Console.WriteLine(ex.Message);
                return StatusCode(500, new { message = "An error occurred during wizard processing.", details = ex.Message });
            }
        }
    }

    public class DashboardCreationRequest
    {
        public string? DashboardName { get; set; }
        public string? ProjectName { get; set; }
        public string? SourceCategory { get; set; }
        public string? ConfluencePageId { get; set; }
    }
}