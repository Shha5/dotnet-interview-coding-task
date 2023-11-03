using Bogus;
using System.Text.Json;
using Users.Persistence;

namespace Users.Console;

internal class Seed
{
    private const int RowsInDatabase = 150;
    private const int ChangeFileSizeInLines = 100;
    private const int ErrorLine = 4000;

    private readonly UserContext _context;
    private readonly string _filePath;

    public Seed(UserContext context, string filePath)
    {
        _context = context;
        _filePath = filePath;
    }

    public async Task Run()
    {
        var random = new Random(9967235);
        Randomizer.Seed = random;
        var userFaker = new Faker<UserProfile>()
            .RuleFor(x => x.FirstName, f => f.Name.FirstName())
            .RuleFor(x => x.LastName, f => f.Name.LastName())
            .RuleFor(x => x.Email, f => f.Internet.Email())
            .RuleFor(x => x.PhoneNumber, f => f.Phone.PhoneNumber())
            .RuleFor(x => x.Address, f => f.Address.FullAddress());

        // populate database
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();

        var users = userFaker.Generate(RowsInDatabase);
        _context.AddRange(users);
        await _context.SaveChangesAsync();
        
        int maxExistingId = users.Max(x => x.Id);

        // populate change file
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }

        using var file = File.CreateText(_filePath);
        for (int i = 0; i < ChangeFileSizeInLines; i++)
        {
            bool insert = random.Next(2) == 1;

            UserProfile user = userFaker.Generate();
            if (i == ErrorLine)
            {
                user.Email = null;
                user.FirstName = new Faker().Random.String(length: 120, 'a', 'z');
            }

            user.Id = insert 
                ? ++maxExistingId 
                : users[random.Next(users.Count)].Id;

            var json = JsonSerializer.Serialize(user);
            await file.WriteLineAsync(json);
        }
    }
}
