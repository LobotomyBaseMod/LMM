Deprecated public classes and public APIs that we need to keep (even if they are now empty and meaningless)
until we change the major version of the package to 3.x.
Because removing any public API can only be done in a major version upgrade, see semantic versioning rules.
Trying to do so in a minor version upgrade of 2.x triggers some automatic detection (API Validation).

In turns, updating the major version of a default package is forbidden in a stable Unity Editor version,
so we cannot do it during the lifetime of Unity 6.x generation and need to wait for the next one.
