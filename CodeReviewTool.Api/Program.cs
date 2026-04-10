using CodeReviewTool.Api.Models;
using CodeReviewTool.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ Register services FIRST
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ReviewService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// ✅ THEN build the app
var app = builder.Build();

app.UseCors("AllowFrontend");

// Middleware
app.UseSwagger();
app.UseSwaggerUI();

// Test endpoint
app.MapGet("/", () => "Code Review Tool API is running");

// Review endpoint
app.MapPost("/review", async (ReviewRequest request, ReviewService service) =>
{
    var result = await service.GenerateAiReview(request.BeforeCode, request.AfterCode);
    return Results.Ok(new { review = result });
});

app.Run();
