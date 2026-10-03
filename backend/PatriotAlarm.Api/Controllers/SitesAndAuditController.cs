using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PatriotAlarm.Api.Data;
using PatriotAlarm.Api.Domain;

namespace PatriotAlarm.Api.Controllers;

[ApiController]
[Route("api/sites")]
public class SitesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SiteDto>>> List(CancellationToken ct)
    {
        var sites = await db.Sites
            .OrderBy(s => s.Name)
            .Select(s => new SiteDto(s.Id, s.AccountNumber, s.Name, s.Address))
            .ToListAsync(ct);
        return Ok(sites);
    }

    [HttpPost]
    public async Task<ActionResult<SiteDto>> Create([FromBody] CreateSiteRequest request, CancellationToken ct)
    {
        if (await db.Sites.AnyAsync(s => s.AccountNumber == request.AccountNumber, ct))
        {
            return Conflict($"Account {request.AccountNumber} already exists.");
        }

        var site = new Site
        {
            AccountNumber = request.AccountNumber,
            Name = request.Name,
            Address = request.Address,
        };
        db.Sites.Add(site);
        await db.SaveChangesAsync(ct);

        return Created($"/api/sites/{site.Id}", new SiteDto(site.Id, site.AccountNumber, site.Name, site.Address));
    }
}

[ApiController]
[Route("api/audit")]
public class AuditController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditDto>>> List(CancellationToken ct)
    {
        var entries = await db.AuditEntries
            .OrderByDescending(a => a.AtUtc)
            .Take(200)
            .Select(a => new AuditDto(a.Id, a.AtUtc, a.Actor, a.Action, a.Details))
            .ToListAsync(ct);
        return Ok(entries);
    }
}
