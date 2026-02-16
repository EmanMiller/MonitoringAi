using DashboardApi.Data;
using DashboardApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DashboardApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogMappingsController : ControllerBase
{
    private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase) { "Environment", "Intent" };

    private readonly ApplicationDbContext _db;

    public LogMappingsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LogMapping>>> GetAll()
    {
        var list = await _db.LogMappings.OrderBy(x => x.Category).ThenBy(x => x.Key).ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LogMapping>> GetById(int id)
    {
        var item = await _db.LogMappings.FindAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<LogMapping>> Create([FromBody] LogMappingDto dto)
    {
        var (validKey, errKey) = InputValidationService.ValidateQueryLibraryKey(dto?.Key);
        if (!validKey) return BadRequest(new { details = errKey });
        var (validVal, errVal) = InputValidationService.ValidateQueryLibraryValue(dto?.Value);
        if (!validVal) return BadRequest(new { details = errVal });
        var category = AllowedCategories.Contains(dto?.Category ?? "") ? dto!.Category!.Trim() : "Environment";
        var keyRaw = (InputValidationService.SanitizeHtmlEntities(dto!.Key) ?? "").Trim();
        var key = keyRaw.Length > 200 ? keyRaw.Substring(0, 200) : keyRaw;
        var entity = new LogMapping
        {
            Category = category,
            Key = key,
            Value = dto.Value ?? "",
            IsActive = true
        };
        _db.LogMappings.Add(entity);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<LogMapping>> Update(int id, [FromBody] LogMappingDto dto)
    {
        var (validKey, errKey) = InputValidationService.ValidateQueryLibraryKey(dto?.Key);
        if (!validKey) return BadRequest(new { details = errKey });
        var (validVal, errVal) = InputValidationService.ValidateQueryLibraryValue(dto?.Value);
        if (!validVal) return BadRequest(new { details = errVal });
        var entity = await _db.LogMappings.FindAsync(id);
        if (entity == null) return NotFound();
        entity.Category = AllowedCategories.Contains(dto?.Category ?? "") ? dto!.Category!.Trim() : entity.Category;
        var keyRaw = (InputValidationService.SanitizeHtmlEntities(dto!.Key) ?? "").Trim();
        entity.Key = keyRaw.Length > 200 ? keyRaw.Substring(0, 200) : keyRaw;
        entity.Value = dto.Value ?? entity.Value;
        if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;
        await _db.SaveChangesAsync();
        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var entity = await _db.LogMappings.FindAsync(id);
        if (entity == null) return NotFound();
        _db.LogMappings.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public class LogMappingDto
{
    public string? Category { get; set; }
    public string? Key { get; set; }
    public string? Value { get; set; }
    public bool? IsActive { get; set; }
}
