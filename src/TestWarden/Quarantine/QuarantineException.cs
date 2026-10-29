namespace TestWarden.Quarantine;

// A quarantine list that can't be read or can't be applied; the run stops with exit code 3.
public sealed class QuarantineException(string message) : Exception(message);
