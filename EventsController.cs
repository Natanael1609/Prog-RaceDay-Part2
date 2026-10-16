using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace RaceDay.Api.Controllers;
[Route("api/events")]
public class EventsController(RaceDayContext db):ApiControllerBase {
 /// <summary>List all events, optionally filtering by status and location.</summary>
 [HttpGet] public async Task<IActionResult> GetAll([FromQuery]string? status,[FromQuery]string? location) {if(status!=null&&!ValidStatus(status))return BadRequest("Invalid status filter.");var q=db.Events.AsQueryable();if(status!=null)q=q.Where(x=>x.Status==status);if(!string.IsNullOrEmpty(location))q=q.Where(x=>x.Location.Contains(location));return Ok(await q.OrderBy(x=>x.EventDate).ToListAsync());}
 /// <summary>Get an event by identifier.</summary>
 [HttpGet("{eventId:int}")] public async Task<IActionResult> Get(int eventId){var item=await db.Events.FindAsync(eventId);return item==null?NotFound():Ok(item);}
 /// <summary>Create a race event (Organiser only).</summary>
 [Authorize(Roles="Organiser")][HttpPost]public async Task<IActionResult> Create(EventDto dto){if(!Validate(dto))return BadRequest("Invalid event details, distance, status or event type.");var e=new RaceEvent{OrganiserId=CurrentUserId,Name=dto.Name,EventDate=dto.EventDate,Location=dto.Location,DistanceKm=dto.DistanceKm,Description=dto.Description,Status=dto.Status,EventType=dto.EventType};db.Events.Add(e);await db.SaveChangesAsync();return CreatedAtAction(nameof(Get),new{eventId=e.EventId},e);}
 /// <summary>Update an event you organise.</summary>
 [Authorize(Roles="Organiser")][HttpPut("{eventId:int}")]public async Task<IActionResult> Update(int eventId,EventDto dto){var e=await db.Events.FindAsync(eventId);if(e==null)return NotFound();if(e.OrganiserId!=CurrentUserId)return Forbid();if(!Validate(dto))return BadRequest("Invalid event details.");e.Name=dto.Name;e.EventDate=dto.EventDate;e.Location=dto.Location;e.DistanceKm=dto.DistanceKm;e.Description=dto.Description;e.Status=dto.Status;e.EventType=dto.EventType;await db.SaveChangesAsync();return Ok(e);}
 /// <summary>Delete an event if no categories/route depend on it.</summary>
 [Authorize(Roles="Organiser")][HttpDelete("{eventId:int}")]public async Task<IActionResult> Delete(int eventId){var e=await db.Events.FindAsync(eventId);if(e==null)return NotFound();if(e.OrganiserId!=CurrentUserId)return Forbid();if(await db.Categories.AnyAsync(x=>x.EventId==eventId)||await db.Routes.AnyAsync(x=>x.EventId==eventId))return Conflict("Event has dependent categories or route.");db.Events.Remove(e);await db.SaveChangesAsync();return NoContent();}
 private static bool Validate(EventDto x)=>!string.IsNullOrWhiteSpace(x.Name)&&!string.IsNullOrWhiteSpace(x.Location)&&x.EventDate!=default&&x.DistanceKm>0&&ValidStatus(x.Status)&&ValidType(x.EventType);
}
