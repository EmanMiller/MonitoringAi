using System.Security.Claims;
using DashboardApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DashboardApi.Models;

namespace DashboardApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _dashboardService;
        private readonly ConfluenceService _confluenceService;
        private readonly IConfiguration _configuration;
        private readonly IActivityService _activityService;
        private readonly DashboardRateLimitService _dashboardRateLimit;

        public DashboardController(
            DashboardService dashboardService,
            ConfluenceService confluenceService,
            IConfiguration configuration,
            IActivityService activityService,
            DashboardRateLimitService dashboardRateLimit)
        {
            _dashboardService = dashboardService;
            _confluenceService = confluenceService;
            _configuration = configuration;
            _activityService = activityService;
            _dashboardRateLimit = dashboardRateLimit;
        }

        [HttpGet("sumo-status")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSumoStatus()
        {
            var (connected, message, folderId) = await _dashboardService.CheckSumoLogicConnectionAsync();
            return Ok(new { connected, message, folderId });
        }

        [HttpGet("confluence-status")]
        [AllowAnonymous]
        public async Task<IActionResult> GetConfluenceStatus()
        {
            var (connected, message) = await _confluenceService.CheckConfluenceConnectionAsync();
            return Ok(new { connected, message });
        }

        [HttpPost]
        public async Task<IActionResult> CreateDashboard([FromBody] DashboardCreationRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            var (allowed, retryAfter) = _dashboardRateLimit.TryConsume(userId);
            if (!allowed)
            {
                Response.Headers.RetryAfter = retryAfter.ToString();
                return StatusCode(429, new { error = "Too many dashboard creations. Please try again later.", retryAfterSeconds = retryAfter });
            }
            try
            {
                var (valid, error) = InputValidationService.ValidateDashboardName(request?.DashboardName);
                if (!valid) return BadRequest(error);
                if (string.IsNullOrEmpty(request?.SourceCategory) || string.IsNullOrEmpty(request?.ConfluencePageId) || string.IsNullOrEmpty(request?.ProjectName))
                    return BadRequest("Request is missing required parameters.");
                var dashboardName = InputValidationService.SanitizeDashboardName(request.DashboardName);

                var dashboardUrl = await _dashboardService.CreateDashboardAsync(dashboardName, request.SourceCategory);
                await _confluenceService.UpdatePageAsync(request.ConfluencePageId, dashboardUrl, request.DashboardName, request.ProjectName);
                _activityService.LogActivity("dashboard_update", $"Dashboard '{dashboardName}' created");
                _activityService.LogActivity("confluence_created", $"New Confluence page: '{request.DashboardName}'");
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
            var (wizardValid, wizardError) = InputValidationService.ValidateWizardRequest(request);
            if (!wizardValid) return BadRequest(wizardError ?? "Invalid wizard request.");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            var (allowed, retryAfter) = _dashboardRateLimit.TryConsume(userId);
            if (!allowed)
            {
                Response.Headers.RetryAfter = retryAfter.ToString();
                return StatusCode(429, new { error = "Too many dashboard creations. Please try again later.", retryAfterSeconds = retryAfter });
            }
            var dashboardTitle = InputValidationService.SanitizeDashboardName(request!.DashboardTitle);

            try
            {
                var wizardRequest = new DashboardWizardRequest
                {
                    DashboardTitle = dashboardTitle,
                    Category = request.Category,
                    UseDefaults = request.UseDefaults,
                    Variables = request.Variables,
                    Panels = request.Panels
                };
                var dashboardUrl = await _dashboardService.CreateDashboardFromWizardAsync(wizardRequest);

                var confluencePageId = _configuration["Confluence:PageId"];
                if (!string.IsNullOrEmpty(confluencePageId))
                {
                    var projectName = "Project From Wizard";
                    await _confluenceService.UpdatePageAsync(confluencePageId, dashboardUrl, dashboardTitle, projectName);
                    _activityService.LogActivity("confluence_created", $"New Confluence page: '{dashboardTitle}'");
                }

                _activityService.LogActivity("dashboard_update", $"Dashboard '{dashboardTitle}' created");
                return Ok(new { dashboardUrl });
            }
            catch (Exception ex)
            {
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