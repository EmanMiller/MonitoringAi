using System.Text.Json;
using DashboardApi.Data;
using DashboardApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DashboardApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueryLibraryController : ControllerBase
{
    private readonly AppDbContext _db;

    public QueryLibraryController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<QueryLibraryItem>>> GetAll([FromQuery] string? category)
    {
        var query = _db.QueryLibrary.OrderBy(x => x.Category).ThenBy(x => x.Key).AsQueryable();
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(x => x.Category == category.Trim());
        var list = await query.ToListAsync();
        return Ok(list);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<QueryLibrarySearchResult>>> Search([FromQuery] string? q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            var all = await _db.QueryLibrary.OrderBy(x => x.Category).ThenBy(x => x.Key).Take(50).ToListAsync();
            return Ok(all.Select(x => new QueryLibrarySearchResult(x, 1.0)));
        }
        var term = q.Trim().ToLowerInvariant();
        var words = term.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        var items = await _db.QueryLibrary.ToListAsync();
        var scored = items
            .Select(item =>
            {
                var keyLower = item.Key.ToLowerInvariant();
                var tags = DeserializeTags(item.TagsJson);
                var tagStr = string.Join(" ", tags).ToLowerInvariant();
                var keySim = Similarity(keyLower, term);
                var tagSim = words.Any(w => tagStr.Contains(w)) ? 0.85 : 0.0;
                var sim = Math.Max(keySim, tagSim);
                if (keySim <= 0 && tagSim <= 0)
                    sim = words.Select(w => Math.Max(Similarity(keyLower, w), tagStr.Contains(w) ? 0.7 : 0)).DefaultIfEmpty(0).Max();
                return (item, sim);
            })
            .Where(t => t.sim > 0.0)
            .OrderByDescending(t => t.sim)
            .ThenByDescending(t => t.item.UsageCount)
            .Take(50)
            .Select(t => new QueryLibrarySearchResult(t.item, t.sim));
        return Ok(scored);
    }

    [HttpGet("popular")]
    public async Task<ActionResult<IEnumerable<QueryLibraryItem>>> GetPopular([FromQuery] int top = 5)
    {
        var list = await _db.QueryLibrary
            .OrderByDescending(x => x.UsageCount)
            .Take(Math.Clamp(top, 1, 20))
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost("{id:guid}/use")]
    public async Task<IActionResult> IncrementUsage(Guid id)
    {
        var item = await _db.QueryLibrary.FindAsync(id);
        if (item == null) return NotFound();
        item.UsageCount++;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QueryLibraryItem>> GetById(Guid id)
    {
        var item = await _db.QueryLibrary.FindAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<QueryLibraryItem>> Create([FromBody] QueryLibraryItemDto dto)
    {
        var (validKey, errKey) = InputValidationService.ValidateQueryLibraryKey(dto?.Key);
        if (!validKey) return BadRequest(new { details = errKey ?? "Key is required." });
        var (validVal, errVal) = InputValidationService.ValidateQueryLibraryValue(dto?.Value);
        if (!validVal) return BadRequest(new { details = errVal ?? "Value contains disallowed content." });
        var keySafe = (InputValidationService.SanitizeHtmlEntities(dto!.Key) ?? "").Trim();
        var tagsJson = SerializeTags(dto.Tags);
        var entity = new QueryLibraryItem
        {
            Id = Guid.NewGuid(),
            Category = (dto.Category ?? "").Trim(),
            Key = keySafe,
            Value = dto.Value ?? "",
            TagsJson = tagsJson,
            CreatedBy = dto.CreatedBy ?? "system",
            CreatedAt = DateTime.UtcNow,
            RoleRequired = dto.RoleRequired ?? "developer"
        };
        _db.QueryLibrary.Add(entity);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<QueryLibraryItem>> Update(Guid id, [FromBody] QueryLibraryItemDto dto)
    {
        var entity = await _db.QueryLibrary.FindAsync(id);
        if (entity == null) return NotFound();
        var (validKey, errKey) = InputValidationService.ValidateQueryLibraryKey(dto?.Key ?? entity.Key);
        if (!validKey) return BadRequest(new { details = errKey ?? "Key is required." });
        var (validVal, errVal) = InputValidationService.ValidateQueryLibraryValue(dto?.Value ?? entity.Value);
        if (!validVal) return BadRequest(new { details = errVal ?? "Value contains disallowed content." });
        entity.Category = (dto.Category ?? entity.Category).Trim();
        entity.Key = (InputValidationService.SanitizeHtmlEntities(dto?.Key ?? entity.Key) ?? "").Trim();
        entity.Value = dto.Value ?? entity.Value;
        entity.TagsJson = SerializeTags(dto.Tags ?? DeserializeTags(entity.TagsJson));
        if (dto.RoleRequired != null) entity.RoleRequired = dto.RoleRequired;
        await _db.SaveChangesAsync();
        return Ok(entity);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var entity = await _db.QueryLibrary.FindAsync(id);
        if (entity == null) return NotFound();
        _db.QueryLibrary.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("export")]
    public async Task<ActionResult<QueryLibraryExport>> Export()
    {
        var items = await _db.QueryLibrary.OrderBy(x => x.Category).ThenBy(x => x.Key).ToListAsync();
        var dtos = items.Select(x => new QueryLibraryItemDto
        {
            Category = x.Category,
            Key = x.Key,
            Value = x.Value,
            Tags = DeserializeTags(x.TagsJson),
            CreatedBy = x.CreatedBy,
            RoleRequired = x.RoleRequired
        }).ToList();
        return Ok(new QueryLibraryExport { ExportedAt = DateTime.UtcNow, Items = dtos });
    }

    [HttpPost("import")]
    public async Task<ActionResult<QueryLibraryImportResult>> Import([FromBody] QueryLibraryExport payload)
    {
        var imported = 0;
        foreach (var dto in payload.Items ?? new List<QueryLibraryItemDto>())
        {
            var tagsJson = SerializeTags(dto.Tags);
            var entity = new QueryLibraryItem
            {
                Id = Guid.NewGuid(),
                Category = dto.Category ?? "",
                Key = (dto.Key ?? "").Trim(),
                Value = dto.Value ?? "",
                TagsJson = tagsJson,
                CreatedBy = dto.CreatedBy ?? "import",
                CreatedAt = DateTime.UtcNow,
                RoleRequired = dto.RoleRequired ?? "developer"
            };
            _db.QueryLibrary.Add(entity);
            imported++;
        }
        await _db.SaveChangesAsync();
        return Ok(new QueryLibraryImportResult { ImportedCount = imported });
    }

    private static double Similarity(string key, string term)
    {
        if (string.IsNullOrEmpty(term)) return 1.0;
        if (key.Contains(term, StringComparison.OrdinalIgnoreCase))
            return 0.9 + (0.1 * (double)term.Length / Math.Max(key.Length, 1));
        var distance = LevenshteinDistance(key, term);
        var maxLen = Math.Max(key.Length, term.Length);
        if (maxLen == 0) return 1.0;
        var similarity = 1.0 - (double)distance / maxLen;
        return similarity >= 0.5 ? similarity : 0;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;
        var m = a.Length;
        var n = b.Length;
        var d = new int[m + 1, n + 1];
        for (var i = 0; i <= m; i++) d[i, 0] = i;
        for (var j = 0; j <= n; j++) d[0, j] = j;
        for (var i = 1; i <= m; i++)
        for (var j = 1; j <= n; j++)
        {
            var cost = a[i - 1] == b[j - 1] ? 0 : 1;
            d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
        }
        return d[m, n];
    }

    private static string SerializeTags(IEnumerable<string>? tags)
    {
        if (tags == null) return "[]";
        return JsonSerializer.Serialize(tags.ToList());
    }

    private static List<string> DeserializeTags(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json);
            return list ?? new List<string>();
        }
        catch { return new List<string>(); }
    }
}

public class QueryLibrarySearchResult
{
    public Guid Id { get; set; }
    public string Category { get; set; } = "";
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string RoleRequired { get; set; } = "";
    public double Confidence { get; set; }

    public QueryLibrarySearchResult(QueryLibraryItem item, double confidence)
    {
        Id = item.Id;
        Category = item.Category;
        Key = item.Key;
        Value = item.Value;
        Tags = DeserializeTags(item.TagsJson);
        CreatedBy = item.CreatedBy;
        CreatedAt = item.CreatedAt;
        RoleRequired = item.RoleRequired;
        Confidence = confidence;
    }

    private static List<string> DeserializeTags(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json);
            return list ?? new List<string>();
        }
        catch { return new List<string>(); }
    }
}

public class QueryLibraryItemDto
{
    public string? Category { get; set; }
    public string? Key { get; set; }
    public string? Value { get; set; }
    public List<string>? Tags { get; set; }
    public string? CreatedBy { get; set; }
    public string? RoleRequired { get; set; }
}

public class QueryLibraryExport
{
    public DateTime ExportedAt { get; set; }
    public List<QueryLibraryItemDto>? Items { get; set; }
}

public class QueryLibraryImportResult
{
    public int ImportedCount { get; set; }
}
