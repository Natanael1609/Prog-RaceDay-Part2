using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using RaceDay.Api;
var builder=WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c=>{
 c.SwaggerDoc("v1",new OpenApiInfo{Title="RaceDay API",Version="v1",Description="Part 2 REST API: register, login, event management, entries, results and route details. Login sets an HttpOnly session cookie for Swagger; JWT bearer is supported too."});
 c.AddSecurityDefinition("Bearer",new OpenApiSecurityScheme{Description="Optional JWT: enter Bearer {token}",Name="Authorization",In=ParameterLocation.Header,Type=SecuritySchemeType.ApiKey});
 c.AddSecurityRequirement(new OpenApiSecurityRequirement{{new OpenApiSecurityScheme{Reference=new OpenApiReference{Type=ReferenceType.SecurityScheme,Id="Bearer"}},Array.Empty<string>()}});
 var xml=Path.Combine(AppContext.BaseDirectory,"RaceDay.Api.xml"); if(File.Exists(xml))c.IncludeXmlComments(xml);
});
builder.Services.AddDbContext<RaceDayContext>(o=>o.UseSqlServer(builder.Configuration.GetConnectionString("RaceDay")));
var key=builder.Configuration["Jwt:Key"]??"";
// Production deployments MUST supply a unique secret via environment variable Jwt__Key.
if(string.IsNullOrWhiteSpace(key)) { if(builder.Environment.IsDevelopment()||builder.Environment.IsEnvironment("Testing"))key="DevelopmentKey-ReplaceForDeployment-2026-RaceDay-Only"; else throw new InvalidOperationException("Set Jwt__Key with a long random secret."); }
var signingKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
builder.Services.AddSingleton(signingKey);
builder.Services.AddAuthentication(options=>{options.DefaultScheme="SmartAuth";options.DefaultChallengeScheme="SmartAuth";})
 .AddPolicyScheme("SmartAuth", "Cookie or Bearer",options=>options.ForwardDefaultSelector=context=>context.Request.Headers.Authorization.ToString().StartsWith("Bearer ",StringComparison.OrdinalIgnoreCase)?JwtBearerDefaults.AuthenticationScheme:CookieAuthenticationDefaults.AuthenticationScheme)
 .AddCookie(options=>{options.Cookie.Name="RaceDay.Auth";options.Cookie.HttpOnly=true;options.Cookie.SecurePolicy=CookieSecurePolicy.SameAsRequest;options.Cookie.SameSite=SameSiteMode.Strict;options.ExpireTimeSpan=TimeSpan.FromHours(1);options.Events.OnRedirectToLogin=c=>{c.Response.StatusCode=401;return Task.CompletedTask;};options.Events.OnRedirectToAccessDenied=c=>{c.Response.StatusCode=403;return Task.CompletedTask;};})
 .AddJwtBearer(options=>{options.TokenValidationParameters=new TokenValidationParameters{ValidateIssuer=true,ValidateAudience=true,ValidateLifetime=true,ValidateIssuerSigningKey=true,ValidIssuer="RaceDay",ValidAudience="RaceDayClients",IssuerSigningKey=signingKey,ClockSkew=TimeSpan.FromMinutes(1)};});
builder.Services.AddAuthorization();builder.Services.AddHttpClient();
var app=builder.Build();
if(app.Environment.IsDevelopment()) {app.UseSwagger();app.UseSwaggerUI();}
app.UseHttpsRedirection();app.UseAuthentication();app.UseAuthorization();app.MapControllers();
app.Run();
public partial class Program {}
