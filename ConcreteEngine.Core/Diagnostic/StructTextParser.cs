using System.Numerics;
using System.Text;
using Core.DebugTools.Utils;

namespace ConcreteEngine.Core.Diagnostic;

internal static class StructTextParser
{
    public static string VectorToText(in Vector3 vector, NumberSpanFormatter formatter, StringBuilder sb)
    {
        return sb.Append('(')
            .Append(formatter.Format(vector.X, "F1")).Append(", ")
            .Append(formatter.Format(vector.Y, "F1")).Append(", ")
            .Append(formatter.Format(vector.Z, "F1")).Append(')')
            .ToString();
    }
}