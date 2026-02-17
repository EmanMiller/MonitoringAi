using DashboardApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DashboardApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConfluenceController : ControllerBase
{
    private readonly ConfluenceService _confluenceService;
    private readonly IConfiguration _configuration;

    public ConfluenceController(ConfluenceService confluenceService, IConfiguration configuration)
    {
        _confluenceService = confluenceService;
        _configuration = configuration;
    }

    /// <summary>Simulate adding a dashboard row to Confluence (no Sumo Logic). For testing Confluence integration.</summary>
    [HttpPost("add-dashboard")]
    public async Task<IActionResult> AddDashboard([FromBody] ConfluenceAddDashboardRequest request)
    {
        var dashboardName = (request?.DashboardName ?? "").Trim();
        var projectName = (request?.ProjectName ?? "").Trim();
        if (string.IsNullOrEmpty(dashboardName))
            return BadRequest(new { error = "Dashboard name is required." });
        if (string.IsNullOrEmpty(projectName))
            return BadRequest(new { error = "Project name is required." });

        var pageId = (request?.ConfluencePageId ?? "").Trim();
        if (string.IsNullOrEmpty(pageId))
            pageId = _configuration["Confluence:PageId"] ?? "";
        if (string.IsNullOrEmpty(pageId))
            return BadRequest(new { error = "Confluence page ID is required. Set CONFLUENCE_PAGE_ID in .env or pass confluencePageId in the request." });

        var dashboardUrl = (request?.DashboardUrl ?? "").Trim();
        if (string.IsNullOrEmpty(dashboardUrl))
            dashboardUrl = "https://sumologic.com/app/dashboards";
        // Prevent javascript: or data: XSS in href
        if (!dashboardUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !dashboardUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            dashboardUrl = "https://sumologic.com/app/dashboards";

        var sanitizedName = InputValidationService.SanitizeDashboardName(dashboardName);
        var sanitizedProject = InputValidationService.SanitizeForDisplay(projectName);
        if (sanitizedProject.Length > 100) sanitizedProject = sanitizedProject[..100];

        try
        {
            await _confluenceService.UpdatePageAsync(pageId, dashboardUrl, sanitizedName, sanitizedProject);
            var baseUrl = (_configuration["Confluence:ApiUrl"] ?? "").TrimEnd('/').Replace("/rest/api", "");
            var pageUrl = !string.IsNullOrEmpty(baseUrl)
                ? $"{baseUrl}/pages/viewpage.action?pageId={pageId}"
                : null;
            return Ok(new { success = true, pageId, pageUrl, message = "Dashboard row added to Confluence." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to update Confluence page.", details = ex.Message });
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] int limit = 10)
    {
        var query = q?.Trim();
        if (string.IsNullOrEmpty(query))
            return Ok(new { results = Array.Empty<object>() });

        var results = await _confluenceService.SearchAsync(query, Math.Clamp(limit, 1, 50));
        return Ok(new { results });
    }
}

public class ConfluenceAddDashboardRequest
{
    public string? DashboardName { get; set; }
    public string? ProjectName { get; set; }
    public string? ConfluencePageId { get; set; }
    public string? DashboardUrl { get; set; }
}
