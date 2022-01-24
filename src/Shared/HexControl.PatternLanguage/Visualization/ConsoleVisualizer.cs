using System;
using System.Collections.Generic;
using HexControl.Core.Buffers.Extensions;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.Visualization;

public class ConsoleVisualizer
{
    private static readonly ConsoleColor[] Colors =
    {
        ConsoleColor.DarkGray,
        ConsoleColor.DarkMagenta,
        ConsoleColor.DarkYellow,
        ConsoleColor.DarkBlue,
        ConsoleColor.DarkGreen,
        ConsoleColor.DarkRed,
        ConsoleColor.DarkCyan
    };

    private int _depth = -1;
    private bool _inArray;

    private bool _previousWasVariable;

    private void Print(string text, bool write = false)
    {
        if (!_previousWasVariable)
        {
            var origColor = Console.ForegroundColor;
            for (var i = 0; i < _depth; i++)
            {
                var color = Colors[i % Colors.Length];
                Console.ForegroundColor = color;
                Console.Write("│   ");
            }

            Console.ForegroundColor = origColor;
        }

        _previousWasVariable = false;

        if (write)
        {
            Console.Write(text);
        }
        else
        {
            Console.WriteLine(text);
        }
    }

    public void Visualize(IEnumerable<PatternData> patterns)
    {
        foreach (var pattern in patterns)
        {
            Visualize(pattern);
        }
    }

    private void Visualize(PatternData patternData)
    {
        _depth++;
        if (patternData is PatternDataStruct @struct)
        {
            VisualizeStruct(@struct);
        }
        else if (patternData is PatternDataUnsigned unsigned)
        {
            VisualizeUnsigned(unsigned);
        }
        else if (patternData is PatternDataSigned signed)
        {
            VisualizeSigned(signed);
        }
        else if (patternData is PatternDataDynamicArray dynamicArray)
        {
            VisualizeDynamicArray(dynamicArray);
        }
        else if (patternData is PatternDataStaticArray staticArray)
        {
            VisualizeStaticArray(staticArray);
        }
        else if (patternData is PatternDataPointer pointer)
        {
            VisualizePointer(pointer);
        }
        else if (patternData is PatternDataEnum @enum)
        {
            VisualizeEnum(@enum);
        }

        _depth--;
    }

    public void VisualizeUnsigned(PatternDataUnsigned pattern)
    {
        VisualizeVariable(pattern, $"({pattern.Offset}, {pattern.Size})");
    }

    public void VisualizeSigned(PatternDataSigned pattern)
    {
        VisualizeVariable(pattern, $"({pattern.Offset}, {pattern.Size})");
    }

    public void VisualizeDynamicArray(PatternDataDynamicArray pattern)
    {
        _inArray = true;
        VisualizeVariable(pattern, write: true, nextNoIndent: true);

        Print("[");
        foreach (var entry in pattern.Entries)
        {
            Visualize(entry);
        }

        Print("]");
        _inArray = false;
    }

    public void VisualizeStruct(PatternDataStruct pattern)
    {
        VisualizeVariable(pattern, write: true, nextNoIndent: true);
        Print("{");
        foreach (var member in pattern.Members)
        {
            Visualize(member);
        }

        Print(_inArray ? "}," : "}");
    }

    public void VisualizePointer(PatternDataPointer pattern)
    {
        var endian = "";
        if (pattern.Endian is Endianess.Little)
        {
            endian = "le ";
        }
        else if (pattern.Endian is Endianess.Big)
        {
            endian = "be ";
        }

        Print($"{endian}[points to: {pattern.PointedAtAddress}]: ", true);
        _previousWasVariable = true;
        if (pattern.PointedAtPattern is not null)
        {
            _depth--;
            Visualize(pattern.PointedAtPattern);
            _depth++;
        }
        else
        {
            Print("undefined");
        }
    }

    public void VisualizeStaticArray(PatternDataStaticArray pattern)
    {
        VisualizeVariable(pattern, $"({pattern.Offset}, {pattern.Size})");
    }

    private void VisualizeVariable(PatternData pattern, string? content = null, bool write = false,
        bool nextNoIndent = false)
    {
        var endian = "";
        if (pattern.Endian is Endianess.Little)
        {
            endian = "le ";
        }
        else if (pattern.Endian is Endianess.Big)
        {
            endian = "be ";
        }

        Print($"{endian}{pattern.GetFormattedName()} {pattern.VariableName} = {content}{(content is null ? "" : ";")}",
            write);
        _previousWasVariable = nextNoIndent;
    }

    public void VisualizeEnum(PatternDataEnum pattern)
    {
        VisualizeVariable(pattern, "IMPLEMENT VALUE");
    }
}