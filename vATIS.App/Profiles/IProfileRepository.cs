using System.Collections.Generic;
using System.Threading.Tasks;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Profiles;

public interface IProfileRepository
{
    Task CheckForProfileUpdates();
    void Save(Profile profile);
    Task Rename(string profileId, string newName);
    Task<List<Profile>> LoadAll();
    Task<Profile> Copy(Profile profile);
    void Delete(Profile profile);
    Task<Profile> Import(string path);
    void Export(Profile profile, string path);
}