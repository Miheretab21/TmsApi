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
//     .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//     .AddCookie(options =>
//     {
//         options.Events.OnRedirectToLogin = context =>
//         {
//             context.Response.StatusCode = 401;
//             return Task.CompletedTask;
//         };
//     });

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

using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Authentication
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
    });

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