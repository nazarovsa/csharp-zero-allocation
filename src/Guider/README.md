# Guider
Sample of optimizing simple guid to efficient string transformer.

Efficient string is a string that represents a guid in user friendly format. For example,
1. Convert GUID to Base64.
2. Replace '/' with '-' (To make it works in url string)
3. Replace '+' with '_' (To make it works in url string)
4. Remove the tailing '=' characters (Because length of our guid is always constant and we can return it back while transforming from string to guid)

## Benchmarks
- **Guider.Benchmark** - Console application with benchmarking. (non-)/zero allocation solutions.
- **Guider.Console** - Console application with example.