// //Exercise 1
// var builder = WebApplication.CreateBuilder(args);
// builder.Services.AddAuthorization();
// var app = builder.Build();

// app.UseRouting();
// // app.UseAuthentication();
// app.UseAuthorization();
// app.MapGet("/api/assessments/results", () => Results.Ok(new
// {
//     courseCode = "CS-101",
//     studentId = "S-001",
//     letterGrade = "A"
// }));

// app.Run();
// you get 	
// courseCode	"CS-101"
// studentId	"S-001"
// letterGrade	"A" on the web


//To get 401
// using Microsoft.AspNetCore.Authentication.Cookies;

// var builder = WebApplication.CreateBuilder(args);

// builder.Services
//     .AddAuthentication("Training")
//     .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);

// builder.Services.AddAuthorization();

// var app = builder.Build();

// app.UseRouting();

// app.UseAuthentication();
// app.UseAuthorization();

// app.MapGet("/api/assessments/results", () => Results.Ok(new
// {
//     courseCode = "CS-101",
//     studentId = "S-001",
//     letterGrade = "A"
// }))
// .RequireAuthorization();

// app.Run();
// we got 401.

//exercise 1b
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Authentication
builder.Services
    .AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);

// Authorization
builder.Services.AddAuthorization();

var app = builder.Build();



app.UseMiddleware<RequestLoggingMiddleware>();


app.UseExceptionHandler("/error");

app.UseHttpsRedirection();



app.UseRouting();


app.UseAuthentication();

app.UseAuthorization();

app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101",
    studentId = "S-001",
    letterGrade = "A"
}))
.RequireAuthorization();

app.Run();