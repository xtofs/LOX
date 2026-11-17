namespace Lox;

using System.Linq;

using Lox.Runtime;
using Lox.Syntax;

public class Interpreter(Logger logger, string sourcePath, int[] lineIndex)
{
    private readonly Logger _logger = logger;
    private readonly int[] _lineIndex = lineIndex;

    private readonly string _sourcePath = sourcePath;

    public object? RetVal { get; private set; }

    public void Eval(Program ast)
    {
        try
        {
            var env = new Environment();
            foreach (var decl in ast.Declarations)
            {
                EvalStmt(decl, env);
            }
        }
        catch (EvalException e)
        {

            Console.WriteLine($"Evaluation error: {e.Message} {_sourcePath}{GetPosition(e.Expr)}");
            Console.WriteLine(e.StackTrace);
        }
    }

    string GetPosition(Expr expr) => GetPosition(expr.Start);

    string GetPosition(int start)
    {
        var ix = _lineIndex.BinarySearch(start);
        if (ix < 0) { ix = ~ix; }
        var line = ix + 1;
        var column = ix == 0 ? start + 1 : start - _lineIndex[ix - 1];
        return $"{_sourcePath}({line}, {column})";
    }


    internal void EvalStmt(Stmt decl, Environment env)
    {
        switch (decl)
        {
            case Stmt.ExprStmt expr:
                var val = EvalExpr(expr.Expression, env);
                break;

            case Stmt.Print print:
                Console.WriteLine(EvalExpr(print.Expression, env));
                break;

            case Stmt.Return ret:
                if (ret.Value is { } some)
                {
                    this.RetVal = EvalExpr(some, env);
                }
                break;
            case Stmt.Block block:
                foreach (var stmt in block.Statements)
                {
                    // TODO: handle early return within block statements
                    EvalStmt(stmt, env);
                }
                break;
            case Stmt.For forStmt:
                EvalForStmt(forStmt, env);
                break;
            case Stmt.ClassDecl classDecl:
                {
                    var super = classDecl.Superclass is { } s && env.TryGetValue(s, out var sc) && sc is Class c ? c : null;
                    var e = env;
                    var methods = classDecl
                        .Methods
                        .Cast<Stmt.FunDecl>()
                        .Select(m =>
                            new Function { Declaration = m, Closure = e, IsInitializer = m.Name == "init" }
                        );
                    var classDef = new Class
                    {
                        Name = classDecl.Name,
                        Superclass = super,
                        Methods = methods.ToDictionary(m => m.Declaration.Name, m => m)
                    };
                    // // bind the class's name to th init method
                    // env.SetVar(classDecl.Name, classDef.Methods.TryGetValue("init", out var initMethod) ? initMethod : Function.Empty);
                    env.SetVar(classDecl.Name, classDef);
                }
                break;

            case Stmt.VarDecl varDecl:
                {
                    var init = EvalExpr(varDecl.Init, env);
                    env.SetVar(varDecl.Name, init);
                }
                break;
            case Stmt.FunDecl funDecl:
                env.SetVar(funDecl.Name,
                    new Function { Declaration = funDecl, Closure = env, IsInitializer = false });
                break;

            default:
                throw new NotImplementedException($"Statement type '{decl.GetType().Name}' not implemented.");
        }
    }

    private void EvalForStmt(Stmt.For forStmt, Environment env)
    {
        // var e = env;
        // bool EvalCondition() => (bool?)EvalExpr(forStmt.Condition, e) != false;

        if (forStmt.Initializer is Stmt.VarDecl varDecl)
        {
            var (var, initExpr) = (varDecl.Name, varDecl.Init);

            env.SetVar(var, EvalExpr(initExpr, env));

            while ((bool?)EvalExpr(forStmt.Condition, env) != false)
            {
                EvalStmt(forStmt.Body, env);
                var val = EvalExpr(forStmt.Increment, env);
                env.SetVar(var, val);
            }
        }
        else
        {
            while ((bool?)EvalExpr(forStmt.Condition, env) != false)
            {
                EvalStmt(forStmt.Body, env);
                var ignored = EvalExpr(forStmt.Increment, env);
            }
        }
    }

    private object? EvalExpr(Expr? expression, Environment env)
    {
        _logger.LogDebug($"Evaluating expression: {expression} {env:KV}");
        switch (expression)
        {
            case Expr.Literal lit:
                return lit.Value;

            case Expr.Variable varExpr:
                {
                    return env.TryGetValue(varExpr.Name, out var value) ? value : throw new EvalException(varExpr, $"Undefined variable '{varExpr.Name}'");
                }
            case Expr.Binary bin:
                var left = EvalExpr(bin.Left, env);
                var right = EvalExpr(bin.Right, env);

                return bin.Op switch
                {
                    "+" => (dynamic)left! + (dynamic)right!,
                    "-" => (dynamic)left! - (dynamic)right!,
                    "*" => (dynamic)left! * (dynamic)right!,
                    "/" => (dynamic)left! / (dynamic)right!,
                    "%" => (dynamic)left! % (dynamic)right!,
                    ">" => (dynamic)left! > (dynamic)right!,
                    "<" => (dynamic)left! < (dynamic)right!,
                    ">=" => (dynamic)left! >= (dynamic)right!,
                    "<=" => (dynamic)left! <= (dynamic)right!,
                    "==" => (dynamic)left! == (dynamic)right!,
                    "!=" => (dynamic)left! != (dynamic)right!,
                    _ => throw new NotSupportedException($"Operator '{bin.Op}' not supported.")
                };

            case Expr.Logical comp:
                var leftComp = EvalExpr(comp.Left, env);
                var rightComp = EvalExpr(comp.Right, env);

                return comp.Op switch
                {
                    ">" => (dynamic)leftComp! > (dynamic)rightComp!,
                    "<" => (dynamic)leftComp! < (dynamic)rightComp!,
                    ">=" => (dynamic)leftComp! >= (dynamic)rightComp!,
                    "<=" => (dynamic)leftComp! <= (dynamic)rightComp!,
                    "==" => (dynamic)leftComp! == (dynamic)rightComp!,
                    "!=" => (dynamic)leftComp! != (dynamic)rightComp!,
                    _ => throw new NotSupportedException($"Operator '{comp.Op}' not supported.")
                };

            case Expr.Assign assign:
                if (assign.Target is Expr.Variable var)
                {
                    var val = EvalExpr(assign.Value, env);
                    _logger.LogDebug($"assigning {val} to {var.Name}");

                    env.SetVar(var.Name, val);
                    return val;
                }
                else if (assign.Target is Expr.GetProperty get)
                {
                    var inst = EvalExpr(get.Object, env);
                    if (inst is not Instance objInst)
                    {
                        throw new EvalException(assign.Target, "Property assignment target must be an instance.");
                    }
                    var val = EvalExpr(assign.Value, env);
                    objInst[get.Name] = val;
                    return val;

                }
                else
                {
                    throw new NotSupportedException($"lhs must be a variable or property. ({assign.Target})");
                }

            case Expr.Call call:
                var callee = EvalExpr(call.Callee, env);
                var arguments = call.Arguments.Select(e => EvalExpr(e, env)).ToArray();

                return callee switch
                {
                    Delegate del =>
                         del.DynamicInvoke(arguments),

                    ICallable callable =>
                       callable.Call(this, arguments),

                    _ =>
                        throw new EvalException(call, $"Callee must be a delegate. {callee}")
                };

            case Expr.GetProperty propGet:
                {
                    var obj = EvalExpr(propGet.Object, env) ?? throw new EvalException(propGet, "Cannot get property of null object.");
                    if (obj is not Instance instance)
                    {
                        throw new EvalException(propGet, $"Cannot get property of {obj.GetType().Name} object {obj}.");
                    }
                    else if (instance.Class.Methods.TryGetValue(propGet.Name, out var method) && method is not null)
                    {
                        method.Bind(instance);
                        return method;
                    }
                    else if (instance.TryGetValue(propGet.Name, out var value))
                    {
                        return value;

                    }
                    else
                    {
                        throw new EvalException(propGet, $"no Property or method named '{propGet.Name}' found on instance of class '{instance.Class.Name}'.");
                    }
                }
            case Expr.This thisExpr:
                {
                    var instance = env.TryGetValue("this", out var val) ? val :
                        throw new EvalException(thisExpr, $"Undefined 'this' variable.");
                    if (instance is not Instance objInst)
                    {
                        throw new EvalException(thisExpr, "'this' must be an instance.");
                    }
                    return objInst;
                }
            default:
                throw new NotImplementedException($"Expression type '{expression?.GetType().Name}' not implemented.");
        }
    }

    internal object? CreateInstance(Class @class, object?[] arguments)
    {
        var newInstance = new Instance { Class = @class };

        _logger.LogDebug($"Creating instance of class: {@class.Name} with arguments: {string.Join(", ", arguments)}");
        // if the class has an initializer method, bind it to the new instance and call it
        if (@class.Methods.TryGetValue("init", out var init) && init is not null)
        {
            init.Bind(newInstance);
            init.Call(this, arguments);
        }

        return newInstance;
    }

    internal object? CallFunction(Function function, object?[] arguments)
    {
        var (declaration, closure) = function;

        var e = new Environment(closure);
        foreach (var (param, arg) in declaration.Parameters.Zip(arguments))
        {
            e.SetVar(param.Name, arg);
        }
        this.EvalStmt(declaration.Body, e);
        // Debug.Assert(e.Equals(closure));

        return this.RetVal;
    }
}

[Serializable]
internal class EvalException(Expr expression, string? message = null) : Exception(message ?? $"Evaluation error at expression: {expression}")
{
    public Expr Expr { get; } = expression;

}