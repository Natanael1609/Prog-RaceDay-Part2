using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api;
using Xunit;
namespace RaceDay.Tests;
public class RaceDayFactory:WebApplicationFactory<Program> {
 private readonly string dbName="test-"+Guid.NewGuid();
 protected override void ConfigureWebHost(IWebHostBuilder builder) {
  builder.UseEnvironment("Testing");
  builder.ConfigureTestServices(s=>{
   var old=s.SingleOrDefault(x=>x.ServiceType==typeof(DbContextOptions<RaceDayContext>));if(old!=null)s.Remove(old);
   s.AddDbContext<RaceDayContext>(o=>o.UseInMemoryDatabase(dbName));
  });
 }
}
public class ApiTests {
 private static async Task<HttpClient> Login(RaceDayFactory factory,string role,string email) {
  var client=factory.CreateClient(new WebApplicationFactoryClientOptions{AllowAutoRedirect=false,HandleCookies=true,BaseAddress=new Uri("https://localhost")});
  var reg=await client.PostAsJsonAsync("/api/auth/register",new{firstName="Test",lastName="User",email,password="Password123!",role});
  Assert.Equal(HttpStatusCode.Created,reg.StatusCode);
  var login=await client.PostAsJsonAsync("/api/auth/login",new{email,password="Password123!"});
  Assert.Equal(HttpStatusCode.OK,login.StatusCode);
  return client;
 }
 private static object Event(string name="Race")=>new{name,eventDate="2027-05-10",location="Cape Town",distanceKm=10,description="Test race",status="Open",eventType="Run"};
 [Fact] public async Task Registration_Succeeds_And_Duplicate_Conflicts(){using var factory=new RaceDayFactory();using var c=factory.CreateClient();var dto=new{firstName="A",lastName="B",email="person@demo.co.za",password="Password123!",role="Participant"};Assert.Equal(HttpStatusCode.Created,(await c.PostAsJsonAsync("/api/auth/register",dto)).StatusCode);Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync("/api/auth/register",dto)).StatusCode);}
 [Fact] public async Task Login_Rejects_WrongPassword(){using var f=new RaceDayFactory();using var c=f.CreateClient();await c.PostAsJsonAsync("/api/auth/register",new{firstName="A",lastName="B",email="login@demo.co.za",password="Password123!",role="Organiser"});var r=await c.PostAsJsonAsync("/api/auth/login",new{email="login@demo.co.za",password="WrongPassword"});Assert.Equal(HttpStatusCode.Unauthorized,r.StatusCode);}
 [Fact] public async Task Unauthenticated_Cannot_Create_Event(){using var f=new RaceDayFactory();using var c=f.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new Uri("https://localhost")});Assert.Equal(HttpStatusCode.Unauthorized,(await c.PostAsJsonAsync("/api/events",Event())).StatusCode);}
 [Fact] public async Task Organiser_Can_Create_And_Update_Event(){using var f=new RaceDayFactory();using var c=await Login(f,"Organiser","organiser@demo.co.za");var r=await c.PostAsJsonAsync("/api/events",Event());Assert.Equal(HttpStatusCode.Created,r.StatusCode);var obj=await r.Content.ReadFromJsonAsync<RaceEvent>();Assert.NotNull(obj);var updated=await c.PutAsJsonAsync($"/api/events/{obj!.EventId}",Event("Updated race"));Assert.Equal(HttpStatusCode.OK,updated.StatusCode);}
 [Fact] public async Task Participant_Is_Forbidden_From_Creating_Event(){using var f=new RaceDayFactory();using var c=await Login(f,"Participant","runner@demo.co.za");Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync("/api/events",Event())).StatusCode);}
 [Fact] public async Task Organiser_Cannot_Enter_And_Participant_Can_Enter(){using var f=new RaceDayFactory();using var org=await Login(f,"Organiser","organiser@demo.co.za");var created=await org.PostAsJsonAsync("/api/events",Event());var ev=await created.Content.ReadFromJsonAsync<RaceEvent>();Assert.NotNull(ev);var category=await org.PostAsJsonAsync($"/api/events/{ev!.EventId}/categories",new{name="10 km Open",maxParticipants=10,entryFee=100});Assert.Equal(HttpStatusCode.Created,category.StatusCode);var cat=await category.Content.ReadFromJsonAsync<Category>();Assert.NotNull(cat);Assert.Equal(HttpStatusCode.Forbidden,(await org.PostAsJsonAsync($"/api/categories/{cat!.CategoryId}/enrolments",new{raceNumber=501})).StatusCode);using var participant=await Login(f,"Participant","participant@demo.co.za");var entry=await participant.PostAsJsonAsync($"/api/categories/{cat.CategoryId}/enrolments",new{raceNumber=501});Assert.Equal(HttpStatusCode.Created,entry.StatusCode);Assert.Equal(HttpStatusCode.Conflict,(await participant.PostAsJsonAsync($"/api/categories/{cat.CategoryId}/enrolments",new{raceNumber=502})).StatusCode);var list=await participant.GetAsync("/api/users/me/enrolments");Assert.Equal(HttpStatusCode.OK,list.StatusCode);Assert.Contains("501",await list.Content.ReadAsStringAsync());}
 [Fact] public async Task Participant_Cannot_View_Organiser_Enrolments(){using var f=new RaceDayFactory();using var p=await Login(f,"Participant","participant@demo.co.za");Assert.Equal(HttpStatusCode.Forbidden,(await p.GetAsync("/api/events/1/enrolments")).StatusCode);}
 [Fact] public async Task Unknown_Event_Returns_NotFound(){using var f=new RaceDayFactory();using var c=f.CreateClient();Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync("/api/events/999")).StatusCode);}
 [Fact] public async Task Invalid_Role_Registration_Rejected(){using var f=new RaceDayFactory();using var c=f.CreateClient();var r=await c.PostAsJsonAsync("/api/auth/register",new{firstName="A",lastName="B",email="badrole@demo.co.za",password="Password123!",role="Admin"});Assert.Equal(HttpStatusCode.BadRequest,r.StatusCode);}
 [Fact] public async Task Participant_Cannot_Record_Results(){using var f=new RaceDayFactory();using var c=await Login(f,"Participant","runner@demo.co.za");Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync("/api/events/1/results",new{enrolmentId=1,finishTime="00:45:00",position=1,paceMinPerKm=4.5,resultStatus="Finished"})).StatusCode);}
}
