namespace Models.Exceptions;

public class RegistrationException : Exception
{
    public RegistrationException(string msg) : base(msg)
    {
        
    }

    public RegistrationException(Type t1, Type t2) : base($"Failed to register insance of {t1} against {t2}")
    {
    }
}