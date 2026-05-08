using CogniCareer.Data;
using CogniCareer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<StudentService>();
builder.Services.AddScoped<JobService>();
builder.Services.AddScoped<ApplicationService>();
builder.Services.AddScoped<CompanyService>();
builder.Services.AddScoped<AlertService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<MatchScoreService>();

var app = builder.Build();

string? connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

#pragma warning disable CS8601 // Possible null reference assignment.
DBHelper.ConnectionString = connectionString;
#pragma warning restore CS8601 // Possible null reference assignment.

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapRazorPages();
app.Run();