using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Net.Mail;
using System.Text.Json;
using Users.Persistence;

namespace Users.Application;

public class UserUpdatesProcessor
{
    private readonly UserContext _context;
    public UserUpdatesProcessor(UserContext context)
    {
        _context = context;
    }

    public async Task<string> Process(StreamReader stream)
    {
        var userProfileJsons = ReadFileIgnoringEmptyLines(stream);
        var userProfiles = DeserializeJonsToValidUserProfiles(userProfileJsons);

        if (userProfiles == null || userProfiles.Count == 0)
        {
            return "Invalid input. Operation couldn't be completed";
        }
        
        var existingUsers = await GetAllProfiles();
        var userProfilesToAdd = new List<UserProfile>();
        var userProfilesToUpdate = new List<UserProfile>();

        foreach (var userProfile in userProfiles)
        {
            if (existingUsers.Find(x => x.Id == userProfile.Id) != null)
            {
                userProfilesToUpdate.Add(userProfile);
            }
            else
            {
                userProfilesToAdd.Add(userProfile);
            }
        }

        try
        {
            await _context.UserProfiles.AddRangeAsync(userProfilesToAdd);
            _context.UserProfiles.UpdateRange(userProfilesToUpdate);
            await _context.SaveChangesAsync();
            return $"Update successfull. Updated {userProfilesToUpdate.Count} recordes and added {userProfilesToAdd.Count} new records.";
        }
        catch (Exception ex)
        {
            return $"Something went wrong. {ex.Message}";
        }
    }

    public async Task<List<UserProfile>> GetAllProfiles()
    {
       var result = await _context.UserProfiles.AsNoTrackingWithIdentityResolution().ToListAsync();
        return result;
    }

    private List<UserProfile> DeserializeJonsToValidUserProfiles(List<string> userProfileJsons)
    {
        var userProfiles = new List<UserProfile>();
        foreach (var userProfileJson in userProfileJsons)
        {
            var userProfile = JsonSerializer.Deserialize<UserProfile>(userProfileJson);
            if (!IsUserValid(userProfile))
            {
                return new List<UserProfile>();
            }

            var duplicateEntry = userProfiles.Find(x => x.Id == userProfile.Id);
            if (duplicateEntry != null)
            {
                userProfiles.Remove(duplicateEntry);
            }
            userProfiles.Add(userProfile);
        }
        return userProfiles;
    }

    private static bool IsUserValid(UserProfile user)
    {
        if (user.Id <= 0
            || string.IsNullOrWhiteSpace(user.LastName)
            || string.IsNullOrWhiteSpace(user.PhoneNumber)
            || string.IsNullOrWhiteSpace(user.Address)
            || string.IsNullOrWhiteSpace(user.FirstName)
            || user.FirstName.Length > 100
            || string.IsNullOrWhiteSpace(user.Email)
            || !MailAddress.TryCreate(user.Email, out _))
        {
            return false;
        }
        
        return true;
    }

    private static List<string> ReadFileIgnoringEmptyLines(StreamReader stream)
    {
        var results = new List<string>();
        var line = stream.ReadLine();
        while (line != null)
        {
            if (line != string.Empty)
            {
                results.Add(line);
                line = stream.ReadLine();
            }
        }
        return results;
    }
}