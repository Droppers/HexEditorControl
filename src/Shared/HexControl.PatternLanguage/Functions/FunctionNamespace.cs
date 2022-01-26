using System.Collections.Generic;
using System.Text;

namespace HexControl.PatternLanguage.Functions;

public class FunctionNamespace
{
    public FunctionNamespace(params string[] name)
    {
        Name = name;
    }

    public IReadOnlyList<string> Name { get; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        for (var i = 0; i < Name.Count; i++)
        {
            sb.Append(Name);

            if (i != Name.Count - 1)
            {
                sb.Append("::");
            }
        }

        return sb.ToString();
    }
}