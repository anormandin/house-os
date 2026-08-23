using HouseOs.Api.Features.Sante;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapSante();

app.Run();
