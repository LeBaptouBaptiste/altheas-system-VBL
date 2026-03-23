namespace API_Althea_systems.Services.IServices;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
    bool MeetsRequirements(string password, out IList<string> errors);
}
