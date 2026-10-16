using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace RaceDay.Api.Controllers;
[Authorize][Route("api/users/me")]
public class UsersController(RaceDayContext db):ApiControllerBase {
 /// <summary>View the logged-in user's profile.</summary>
 [HttpGet] public async Task<IActionResult> Me(){var u=await db.Users.FindAsync(CurrentUserId);return u==null?NotFound():Ok(new{u.UserId,u.FirstName,u.LastName,u.Email,u.Phone,u.Role,u.CreatedAt});}
 /// <summary>Update personal details without changing role or account identity.</summary>
 [HttpPut] public async Task<IActionResult> Update(ProfileDto dto){var u=await db.Users.FindAsync(CurrentUserId);if(u==null)return NotFound();if(string.IsNullOrWhiteSpace(dto.FirstName)||string.IsNullOrWhiteSpace(dto.LastName))return BadRequest("Name required.");u.FirstName=dto.FirstName.Trim();u.LastName=dto.LastName.Trim();u.Phone=dto.Phone;await db.SaveChangesAsync();return Ok(new{u.UserId,u.FirstName,u.LastName,u.Email,u.Phone,u.Role});}
}
