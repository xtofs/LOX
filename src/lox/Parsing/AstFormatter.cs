using System.Collections;
using System.Text;

namespace Lox;


public static class AstFormatter
{
    public static string Format(Program program)
    {
        var sb = new StringBuilder();
        Format(sb, program, "", true);
        return sb.ToString();
    }

    private static void Format(StringBuilder sb, Program program, string indent, bool isLast)
    {
        AppendPrefix(sb, ref indent, isLast);
        sb.AppendLine($"Program");

        foreach (var (child, isLastChild) in WithLast(program.Declarations))
        {
            Format(sb, child, indent, isLastChild);
        }
    }

    private static void Format(StringBuilder sb, Stmt? statement, string indent, bool isLast)
    {
        AppendPrefix(sb, ref indent, isLast);

        if (statement is null)
        {
            sb.AppendLine($"Null Stmt");
            return;
        }

        switch (statement)
        {
            case Stmt.ExprStmt expr:
                sb.AppendLine($"Expr");
                Format(sb, expr.Expression, indent, true);
                break;
            case Stmt.Print print:
                sb.AppendLine($"Print");
                Format(sb, print.Expression, indent, true);
                break;
            case Stmt.Return ret:
                sb.AppendLine($"Return");
                Format(sb, ret.Value, indent, true);
                break;
            case Stmt.Block block:
                sb.AppendLine($"Block");
                foreach (var (child, isLastChild) in WithLast(block.Statements))
                {
                    Format(sb, child, indent, isLastChild);
                }
                break;
            case Stmt.If ifStmt:
                sb.AppendLine($"If");
                break;
            case Stmt.While whileStmt:
                sb.AppendLine($"While");
                break;
            case Stmt.For forStmt:
                sb.AppendLine($"For");
                // FormatFixed<Stmt>(sb, [forStmt.Initializer, forStmt.Condition, forStmt.Increment], indent);
                Format(sb, forStmt.Initializer, indent, false);
                Format(sb, forStmt.Condition, indent, false);
                Format(sb, forStmt.Increment, indent, false);
                Format(sb, forStmt.Body, indent, true);

                break;
            case Stmt.VarDecl varDecl:
                sb.AppendLine($"VarDecl {varDecl.Name}");
                Format(sb, varDecl.Init, indent, true);
                break;
            case Stmt.FunDecl func:
                sb.AppendLine($"Fun {func.Name}");
                Format(sb, func.Parameters.Select(p => p.Name).ToList(), indent, false);
                Format(sb, func.Body, indent, true);
                break;
            case Stmt.ClassDecl classDecl:
                sb.AppendLine($"Class {classDecl.Name} {classDecl.Superclass}");
                foreach (var (method, isLastMethod) in WithLast(classDecl.Methods))
                {
                    Format(sb, method, indent, isLastMethod);
                }
                break;

            default:
                sb.AppendLine($"Unknown Stmt \e[31m{statement.GetType().Name}\e[0m");
                break;
        }

    }

    private static void Format(StringBuilder sb, IReadOnlyList<string> parameters, string indent, bool isLast)
    {
        AppendPrefix(sb, ref indent, isLast);
        sb.AppendLine("Parameters");
        foreach (var (param, isLastParam) in WithLast(parameters))
        {
            sb.AppendLine($"{indent}{(isLastParam ? Corner : Cross)}Param {param}");
        }
    }

    private static void FormatFixed<T>(StringBuilder sb, T[] values, string indent)
    {
        throw new NotImplementedException();
    }

    private static void Format(StringBuilder sb, Expr? expression, string indent, bool isLast)
    {
        AppendPrefix(sb, ref indent, isLast);
        if (expression is null)
        {
            sb.AppendLine($"Null Expr");
            return;
        }
        switch (expression)
        {
            case Expr.Literal lit:
                sb.AppendLine($"Literal {lit.Value}");
                break;
            case Expr.Variable varExpr:
                sb.AppendLine($"Variable {varExpr.Name}");
                break;
            case Expr.Binary bin:
                sb.AppendLine($"Binary {bin.Op}");
                Format(sb, bin.Left, indent, false);
                Format(sb, bin.Right, indent, true);
                break;
            case Expr.Unary unary:
                sb.AppendLine($"Unary {unary.Op}");
                break;
            case Expr.Call call:
                sb.AppendLine($"Call {call.Callee}");
                foreach (var (arg, isLastArg) in WithLast(call.Arguments))
                {
                    Format(sb, arg, indent, isLastArg);
                }
                break;
            case Expr.Assign assign:
                sb.AppendLine($"Assign");
                Format(sb, assign.Target, indent, false);
                Format(sb, assign.Value, indent, true);
                break;
            case Expr.GetProperty getProp:
                sb.AppendLine($"GetProperty {getProp.Name}");
                Format(sb, getProp.Object, indent, true);
                break;
            default:
                sb.AppendLine($"Unknown Expr \e[31m{expression.GetType().Name}\e[0m");
                break;
        }

    }

    private static void AppendPrefix(StringBuilder sb, ref string indent, bool isLast)
    {
        sb.Append(indent);
        sb.Append(isLast ? Corner : Cross);
        sb.Append(' ');
        indent += isLast ? Space : Vertical;
    }


    // Constants for drawing lines and spaces
    private const string Cross = " ├─";
    private const string Corner = " └─";
    private const string Vertical = " │ ";
    private const string Space = "   ";



    private static Iterator<T> WithLast<T>(IReadOnlyList<T> list) => new Iterator<T>(list);

    private struct Iterator<T>(IReadOnlyList<T> list)
    {
        public readonly Iterator<T> GetEnumerator() => this;

        private readonly IReadOnlyList<T> _list = list;
        private int _index = -1;

        public bool MoveNext() => ++_index < _list.Count;

        public readonly (T, bool) Current => (_list[_index], _index == _list.Count - 1);
    }
}

