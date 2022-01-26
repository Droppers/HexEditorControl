using System.Collections.Generic;
using HexControl.PatternLanguage.Literals;

namespace HexControl.PatternLanguage.Functions;

public delegate Literal? FunctionBody(Evaluator ctx, IReadOnlyList<Literal> parameters);