# TestWarden

The exit code you can trust for Microsoft Testing Platform (MTP) tests in CI.

TestWarden runs MTP test projects and turns what happened into one reliable exit code:

- Quarantined tests are skipped, whatever the test framework.
- A crash after a clean run passes only with proof that every test finished.
- A crash during the run always fails, on Linux and Windows.

**Status:** feasibility spikes. Nothing is published yet.

## License

MIT
