namespace API_Althea_systems.Common.Exceptions;

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }

    public ConflictException(string entityName, string field, string value)
        : base($"{entityName} with {field} '{value}' already exists.") { }
}
