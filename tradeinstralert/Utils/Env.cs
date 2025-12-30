namespace tradeinstralert.Utils;

public static class Env
{
    public static string Require(string name)
        => Environment.GetEnvironmentVariable(name)
           ?? throw new InvalidOperationException($"Missing environment variable: {name}");

    public static string Get(string name, string @default)
        => Environment.GetEnvironmentVariable(name) ?? @default;
}
