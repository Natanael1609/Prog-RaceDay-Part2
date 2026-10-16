using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace RaceDay.Api.Controllers;
[ApiController]
public abstract class ApiControllerBase:ControllerBase {
 protected int CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:0;
 protected bool IsOrganiser => User.IsInRole("Organiser");
 protected bool IsParticipant => User.IsInRole("Participant");
 protected static bool ValidStatus(string s)=>new[]{"Draft","Upcoming","Open","Completed","Cancelled"}.Contains(s);
 protected static bool ValidType(string s)=>new[]{"Run","Walk","Cycle"}.Contains(s);
}
