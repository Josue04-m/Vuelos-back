using Npgsql;

namespace Vuelos_Backend.Data;

internal static class ErroresPostgreSql
{
    public static PostgresException? Obtener(Exception error)
    {
        for (Exception? current = error; current is not null; current = current.InnerException)
            if (current is PostgresException postgres) return postgres;
        return null;
    }

    public static bool EsConcurrencia(Exception error) => Obtener(error)?.SqlState is
        PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.LockNotAvailable;

    public static bool EsConflicto(Exception error) => EsConcurrencia(error) || Obtener(error)?.SqlState is
        PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation or
        PostgresErrorCodes.CheckViolation or PostgresErrorCodes.ExclusionViolation;

    public static bool EsUnico(Exception error, string constraint) =>
        Obtener(error) is { SqlState: PostgresErrorCodes.UniqueViolation } postgres &&
        string.Equals(postgres.ConstraintName, constraint, StringComparison.Ordinal);
}
