using DashboardApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DashboardApi.Models;
using System;

namespace DashboardApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _dashboardService;
        private readonly ConfluenceService _confluenceService;
        private readonly IConfiguration _configuration;
        private readonly IActivityService _activityService;

        public DashboardController(
            DashboardService dashboardService,
            ConfluenceService confluenceService,
            IConfiguration configuration,
            IActivityService activityService)
        {
            _dashboardService = dashboardService;
            _confluenceService = confluenceService;
            _configuration = configuration;
            _activityService = activityService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateDashboard([FromBody] DashboardCreationRequest request)
        {
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
            var (valid, error) = InputValidationService.ValidateDashboardName(request?.DashboardTitle);
            if (!valid) return BadRequest(error ?? "Request is missing required parameters.");
            var dashboardTitle = InputValidationService.SanitizeDashboardName(request!.DashboardTitle);

            try
            {
                var wizardRequest = new DashboardWizardRequest
                {
                    DashboardTitle = dashboardTitle,
                    UseDefaults = request.UseDefaults,
                    Variables = request.Variables,
                    Panels = request.Panels
                };
                var dashboardUrl = await _dashboardService.CreateDashboardFromWizardAsync(wizardRequest);

                var confluencePageId = _configuration["Confluence:PageId"];
                if (string.IsNullOrEmpty(confluencePageId))
                    return StatusCode(500, "Confluence PageId is not configured.");

                var projectName = "Project From Wizard";
                await _confluenceService.UpdatePageAsync(confluencePageId, dashboardUrl, dashboardTitle, projectName);
                _activityService.LogActivity("dashboard_update", $"Dashboard '{dashboardTitle}' updated");
                _activityService.LogActivity("confluence_created", $"New Confluence page: '{dashboardTitle}'");

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