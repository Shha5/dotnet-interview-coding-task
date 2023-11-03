using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Users.Application;
using Users.Console;
using Users.Persistence;

var services = new ServiceCollection();
services.AddDbContext<UserContext>(options =>
{
    options.UseSqlite(@"Data Source=Users.db;");
    options.EnableSensitiveDataLogging();
});

services.AddTransient<UserUpdatesProcessor>();

IServiceProvider sp = services.BuildServiceProvider();

string path = "user_updates.jsonl";

using (var scope = sp.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<UserContext>();
    await new Seed(dbContext, path).Run();
}

var processor = sp.GetRequiredService<UserUpdatesProcessor>();

foreach (var profile in await processor.GetAllProfiles())
{
    Console.WriteLine($"{profile.Id} {profile.FirstName} {profile.LastName} {profile.Email} {profile.PhoneNumber} {profile.Address}");
}

using (var reader = new StreamReader(path))
{
    var result = await processor.Process(reader);
    Console.WriteLine(result);
    Console.ReadKey();
}

Console.WriteLine("After update");
var profiles = await processor.GetAllProfiles();
foreach (var profile in profiles)
{
    Console.WriteLine($"{profile.Id} {profile.FirstName} {profile.LastName} {profile.Email} {profile.PhoneNumber} {profile.Address}");
}
Console.ReadKey();