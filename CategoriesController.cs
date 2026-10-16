using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace RaceDay.Api.Controllers;
[Route("api")]
public class CategoriesController(RaceDayContext db):ApiControllerBase {
 /// <summary>List categories for an event.</summary>
 [HttpGet("events/{eventId:int}/categories")]public async Task<IActionResult> GetAll(int eventId){if(!await db.Events.AnyAsync(x=>x.EventId==eventId))return NotFound();return Ok(await db.Categories.Where(x=>x.EventId==eventId).ToListAsync());}
 /// <summary>Define an entry category for your event.</summary>
 [Authorize(Roles="Organiser")][HttpPost("events/{eventId:int}/categories")]public async Task<IActionResult> Create(int eventId,CategoryDto dto){var ev=await db.Events.FindAsync(eventId);if(ev==null)return NotFound();if(ev.OrganiserId!=CurrentUserId)return Forbid();if(!Valid(dto))return BadRequest("Invalid category.");if(await db.Categories.AnyAsync(x=>x.EventId==eventId&&x.Name==dto.Name))return Conflict("Category name already exists.");var c=new Category{EventId=eventId,Name=dto.Name,MaxParticipants=dto.MaxParticipants,EntryFee=dto.EntryFee};db.Categories.Add(c);await db.SaveChangesAsync();return StatusCode(201,c);}
 /// <summary>Update a category belonging to your event.</summary>
 [Authorize(Roles="Organiser")][HttpPut("categories/{categoryId:int}")]public async Task<IActionResult> Update(int categoryId,CategoryDto dto){var c=await db.Categories.Include(x=>x.Event).FirstOrDefaultAsync(x=>x.CategoryId==categoryId);if(c==null)return NotFound();if(c.Event.OrganiserId!=CurrentUserId)return Forbid();if(!Valid(dto))return BadRequest("Invalid category.");if(await db.Categories.AnyAsync(x=>x.EventId==c.EventId&&x.Name==dto.Name&&x.CategoryId!=categoryId))return Conflict("Category exists.");if(dto.MaxParticipants<await db.Enrolments.CountAsync(x=>x.CategoryId==categoryId&&x.Status!="Cancelled"))return Conflict("Capacity less than current entries.");c.Name=dto.Name;c.MaxParticipants=dto.MaxParticipants;c.EntryFee=dto.EntryFee;await db.SaveChangesAsync();return Ok(c);}
 /// <summary>Remove a category with no enrolments.</summary>
 [Authorize(Roles="Organiser")][HttpDelete("categories/{categoryId:int}")]public async Task<IActionResult> Delete(int categoryId){var c=await db.Categories.Include(x=>x.Event).FirstOrDefaultAsync(x=>x.CategoryId==categoryId);if(c==null)return NotFound();if(c.Event.OrganiserId!=CurrentUserId)return Forbid();if(await db.Enrolments.AnyAsync(x=>x.CategoryId==categoryId))return Conflict("Category has enrolments.");db.Categories.Remove(c);await db.SaveChangesAsync();return NoContent();}
 private static bool Valid(CategoryDto dto)=>!string.IsNullOrWhiteSpace(dto.Name)&&dto.MaxParticipants>0&&dto.EntryFee>=0;
}
