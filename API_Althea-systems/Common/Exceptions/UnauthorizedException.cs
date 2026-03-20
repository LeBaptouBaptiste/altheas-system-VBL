namespace API_Althea_systems.Common.Exceptions;

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Unauthorized access.")
        : base(message) { }
}
