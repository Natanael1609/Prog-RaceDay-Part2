using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
namespace RaceDay.Api.Controllers;
[Route("api/events/{eventId:int}/weather")]
public class WeatherController(RaceDayContext db,IHttpClientFactory factory,IConfiguration config):ApiControllerBase {
 /// <summary>Fetch forecast weather for an event location. Supply Weather__ApiKey from OpenWeather.</summary>
 [HttpGet]public async Task<IActionResult> Get(int eventId){var ev=await db.Events.FindAsync(eventId);if(ev==null)return NotFound();var key=config["Weather:ApiKey"];if(string.IsNullOrWhiteSpace(key))return StatusCode(502,new{message="Weather provider not configured. Set Weather__ApiKey."});try{var client=factory.CreateClient();var url=$"https://api.openweathermap.org/data/2.5/weather?q={Uri.EscapeDataString(ev.Location)}&appid={Uri.EscapeDataString(key)}&units=metric";using var response=await client.GetAsync(url);if(!response.IsSuccessStatusCode)return StatusCode(502,new{message="Weather provider unavailable."});using var json=await response.Content.ReadFromJsonAsync<System.Text.Json.JsonDocument>();return json==null?StatusCode(502):Ok(json.RootElement.Clone());}catch(HttpRequestException){return StatusCode(502,new{message="Weather provider unavailable."});}catch(TaskCanceledException){return StatusCode(502,new{message="Weather provider timed out."});}}
}
