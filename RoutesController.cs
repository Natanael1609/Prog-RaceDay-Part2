using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace RaceDay.Api.Controllers;
[Route("api/events/{eventId:int}/route")]
public class RoutesController(RaceDayContext db):ApiControllerBase {
 /// <summary>Get the route details for an event.</summary>
 [HttpGet]public async Task<IActionResult> Get(int eventId){if(!await db.Events.AnyAsync(x=>x.EventId==eventId))return NotFound();var r=await db.Routes.FirstOrDefaultAsync(x=>x.EventId==eventId);return r==null?NotFound():Ok(new{r.RouteId,r.EventId,r.StartPoint,r.FinishPoint,r.RouteUrl,r.ElevationGainM,r.Notes});}
 /// <summary>Create or replace the route for an event you organise.</summary>
 [Authorize(Roles="Organiser")][HttpPut]public async Task<IActionResult> Upsert(int eventId,RouteDto dto){var ev=await db.Events.FindAsync(eventId);if(ev==null)return NotFound();if(ev.OrganiserId!=CurrentUserId)return Forbid();if(string.IsNullOrWhiteSpace(dto.StartPoint)||string.IsNullOrWhiteSpace(dto.FinishPoint)||dto.ElevationGainM<0)return BadRequest("Invalid route.");var r=await db.Routes.FirstOrDefaultAsync(x=>x.EventId==eventId);if(r==null){r=new RaceRoute{EventId=eventId};db.Routes.Add(r);}r.StartPoint=dto.StartPoint;r.FinishPoint=dto.FinishPoint;r.RouteUrl=dto.RouteUrl;r.ElevationGainM=dto.ElevationGainM;r.Notes=dto.Notes;await db.SaveChangesAsync();return Ok(new{r.RouteId,r.EventId,r.StartPoint,r.FinishPoint,r.RouteUrl,r.ElevationGainM,r.Notes});}
}
