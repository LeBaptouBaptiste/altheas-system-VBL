namespace API_Althea_systems.Common.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "Access forbidden.")
        : base(message) { }
}
