namespace HexControl.PatternLanguage.Functions;

public record FunctionDefinition(FunctionNamespace? Namespace, string Name, FunctionParameterCount ParameterCount,
    FunctionBody Body, bool Dangerous = false);