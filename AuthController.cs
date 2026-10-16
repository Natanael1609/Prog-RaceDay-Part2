using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
namespace RaceDay.Api.Controllers;
[Route("api/auth")]
public class AuthController(RaceDayContext db,SymmetricSecurityKey key):ApiControllerBase {
 private readonly PasswordHasher<User> hasher=new();
 /// <summary>Create an Organiser or Participant account. Both roles are allowed for this coursework prototype.</summary>
 [HttpPost("register")]
 public async Task<IActionResult> Register(RegisterDto request) {
  if(!new[]{"Organiser","Participant"}.Contains(request.Role))return BadRequest("Role must be Organiser or Participant.");
  if(string.IsNullOrWhiteSpace(request.FirstName)||string.IsNullOrWhiteSpace(request.LastName)||request.Password.Length<8)return BadRequest("Enter valid details and a password of at least 8 characters.");
  var email=request.Email.Trim().ToLowerInvariant(); if(await db.Users.AnyAsync(x=>x.Email==email))return Conflict("Email already registered.");
  var user=new User{FirstName=request.FirstName.Trim(),LastName=request.LastName.Trim(),Email=email,Phone=request.Phone,Role=request.Role};user.PasswordHash=hasher.HashPassword(user,request.Password);db.Users.Add(user);await db.SaveChangesAsync();return StatusCode(201,new{userId=user.UserId,message="Registration successful"});
 }
 /// <summary>Login using email/password. Sets a secure HttpOnly cookie and also returns a bearer token.</summary>
 [HttpPost("login")]
 public async Task<IActionResult> Login(LoginDto request) {
  var user=await db.Users.FirstOrDefaultAsync(x=>x.Email==request.Email.Trim().ToLowerInvariant());
  if(user==null||hasher.VerifyHashedPassword(user,user.PasswordHash,request.Password)==PasswordVerificationResult.Failed)return Unauthorized("Invalid email or password.");
  var claims=new[]{new Claim(ClaimTypes.NameIdentifier,user.UserId.ToString()),new Claim(ClaimTypes.Name,user.Email),new Claim(ClaimTypes.Role,user.Role)};
  await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)),new AuthenticationProperties{IsPersistent=false,ExpiresUtc=DateTimeOffset.UtcNow.AddHours(1)});
  var token=new JwtSecurityToken("RaceDay","RaceDayClients",claims,expires:DateTime.UtcNow.AddHours(1),signingCredentials:new SigningCredentials(key,SecurityAlgorithms.HmacSha256));
  return Ok(new{token=new JwtSecurityTokenHandler().WriteToken(token),expiresIn=3600,role=user.Role});
 }
 /// <summary>End the browser session.</summary>
 [HttpPost("logout")]
 public async Task<IActionResult> Logout(){await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);return Ok(new{message="Logged out"});}
}
