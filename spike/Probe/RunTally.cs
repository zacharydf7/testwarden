namespace Probe;

// Final state per test node, so a test reported as in progress and then passed counts once.
public sealed class RunTally
{
    private readonly Dictionary<string, string> _outcomes = [];

    public void Record(string uid, string outcome)
    {
        lock (_outcomes)
        {
            _outcomes[uid] = outcome;
        }
    }

    public int Count(string outcome)
    {
        lock (_outcomes)
        {
            return _outcomes.Values.Count(o => o == outcome);
        }
    }
}
