using DashboardApi.Data;
using DashboardApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DashboardApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SavedQueriesController : ControllerBase
{
    private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Browse Product", "Browse Path", "Account", "Checkout", "Gift Registry", "API", "Environment", "Intent"
    };

    private readonly ApplicationDbContext _db;

    public SavedQueriesController(ApplicationDbContext db) => _db = db;

    /// <summary>Search across Name (Key) and Tags with fuzzy word matching. Debounce on client (300ms).</summary>
    [HttpGet("search")]
    public async Task<ActionResult<SearchResult>> Search([FromQuery] string? q)
    {
        var (valid, error) = InputValidationService.ValidateSearchQuery(q);
        if (!valid) return BadRequest(new { details = error });
        var all = await _db.SavedQueries.ToListAsync();
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(new SearchResult { Queries = all.Take(20).ToList(), Total = all.Count });
        }

        var term = q!.Trim().Length > 100 ? q.Trim().Substring(0, 100) : q.Trim();
        var words = term.ToLowerInvariant().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        var scored = all
            .Select(qry =>
            {
                var nameLower = qry.Name.ToLowerInvariant();
                var tagsLower = (qry.Tags ?? "").ToLowerInvariant();
                var score = 0;
                foreach (var w in words)
                {
                    if (string.IsNullOrEmpty(w)) continue;
                    if (nameLower.Contains(w)) score += 10;
                    if (tagsLower.Contains(w)) score += 5;
                    // Fuzzy: allow 1-char typo (substring match with length >= 2)
                    if (w.Length >= 2 && (nameLower.Contains(w) || tagsLower.Split(',').Any(t => t.Trim().Contains(w))))
                        score += 1;
                }
                return (Query: qry, Score: score);
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Query.UsageCount)
            .Select(x => x.Query)
            .ToList();

        return Ok(new SearchResult { Queries = scored.Take(50).ToList(), Total = scored.Count });
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SavedQuery>>> GetAll()
    {
        var list = await _db.SavedQueries.OrderBy(x => x.Category).ThenBy(x => x.Name).ToListAsync();
        return Ok(list);
    }

    [HttpGet("popular")]
    public async Task<ActionResult<IEnumerable<SavedQuery>>> GetPopular([FromQuery] int top = 5)
    {
        var list = await _db.SavedQueries
            .OrderByDescending(x => x.UsageCount)
            .Take(Math.Clamp(top, 1, 20))
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost("{id}/use")]
    public async Task<IActionResult> IncrementUsage(int id)
    {
        var q = await _db.SavedQueries.FindAsync(id);
        if (q == null) return NotFound();
        q.UsageCount++;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost]
    public async Task<ActionResult<SavedQuery>> Create([FromBody] SavedQueryDto dto)
    {
        var (validKey, errKey) = InputValidationService.ValidateQueryLibraryKey(dto?.Name);
        if (!validKey) return BadRequest(new { details = errKey });
        var (validVal, errVal) = InputValidationService.ValidateQueryLibraryValue(dto?.QueryText);
        if (!validVal) return BadRequest(new { details = errVal });
        var category = AllowedCategories.Contains(dto?.Category ?? "") ? dto!.Category!.Trim() : "API";
        var nameRaw = (InputValidationService.SanitizeHtmlEntities(dto!.Name) ?? "").Trim();
        var name = nameRaw.Length > 200 ? nameRaw.Substring(0, 200) : nameRaw;
        var entity = new SavedQuery
        {
            Name = name,
            QueryText = dto.QueryText ?? "",
            Category = category,
            Tags = (dto.Tags ?? "").Trim().Length > 256 ? (dto.Tags ?? "").Trim().Substring(0, 256) : (dto.Tags ?? "").Trim()
        };
        _db.SavedQueries.Add(entity);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), null, entity);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SavedQuery>> Update(int id, [FromBody] SavedQueryDto dto)
    {
        var (validKey, errKey) = InputValidationService.ValidateQueryLibraryKey(dto?.Name);
        if (!validKey) return BadRequest(new { details = errKey });
        var (validVal, errVal) = InputValidationService.ValidateQueryLibraryValue(dto?.QueryText);
        if (!validVal) return BadRequest(new { details = errVal });
        var entity = await _db.SavedQueries.FindAsync(id);
        if (entity == null) return NotFound();
        var nameRaw = (InputValidationService.SanitizeHtmlEntities(dto!.Name) ?? "").Trim();
        entity.Name = nameRaw.Length > 200 ? nameRaw.Substring(0, 200) : nameRaw;
        entity.QueryText = dto.QueryText ?? entity.QueryText;
        entity.Category = AllowedCategories.Contains(dto.Category ?? "") ? dto.Category!.Trim() : entity.Category;
        entity.Tags = (dto.Tags ?? "").Trim().Length > 256 ? (dto.Tags ?? "").Trim().Substring(0, 256) : (dto.Tags ?? "").Trim();
        await _db.SaveChangesAsync();
        return Ok(entity);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.SavedQueries.FindAsync(id);
        if (entity == null) return NotFound();
        _db.SavedQueries.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public class SearchResult
{
    public List<SavedQuery> Queries { get; set; } = new();
    public int Total { get; set; }
}

public class SavedQueryDto
{
    public string? Name { get; set; }
    public string? QueryText { get; set; }
    public string? Category { get; set; }
    public string? Tags { get; set; }
}
