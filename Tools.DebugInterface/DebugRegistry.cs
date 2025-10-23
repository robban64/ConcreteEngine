#region

using System.Linq.Expressions;
using System.Reflection;
using Tools.DebugInterface.Data;

#endregion

namespace Tools.DebugInterface;

public sealed class DebugRegistry
{
    private readonly Dictionary<string, Func<object?, object?[], object?>> _commands =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, DebugWatchRecord> _watches =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> CommandNames => _commands.Keys;
    public IReadOnlyCollection<string> WatchNames => _watches.Keys;

    // API
    public object? Read(string name) => _watches[name].Read(null);
    public object? Read(string name, object? target) => _watches[name].Read(target);
    public object? Read(string name, WeakReference target) => _watches[name].Read(target.Target);

    public object? ReadBound(string name)
    {
        var target = StaticDebugProvider.Resolve(name);
        return _watches[name].Read(target);
    }

    public void Write(string name, object? value) => _watches[name].Write(null, value);
    public void Write(string name, object? target, object? value) => _watches[name].Write(target, value);
    public void Write(string name, WeakReference target, object? value) => _watches[name].Write(target.Target, value);

    public object? ExecuteBound(string name, params object?[] args)
    {
        var target = StaticDebugProvider.Resolve(name);
        return _commands[name](target, args);
    }

    // Commands
    public object? Execute(string name, object? target = null, params object?[] args)
    {
        var inv = _commands[name];
        return inv(target, args);
    }

    public object? Execute(string name) => Execute(name, null);

    public T Execute<T>(string name, object? target = null, params object?[] args) => (T)Execute(name, target, args)!;

    public void RegisterFromAssemblies(Assembly asm, params string[]? namespaceStrings)
    {
        if (namespaceStrings is null || namespaceStrings.Length == 0)
            return;

        HashSet<string> set = new(namespaceStrings);
        foreach (var t in asm.DefinedTypes)
        {
            var ns = t.Namespace;
            if (ns is not null && set.Contains(ns))
                RegisterType(t);
        }
    }

    private void RegisterType(Type t)
    {
        const BindingFlags flags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

        // commands
        var methods = t.GetMethods(flags);
        foreach (var m in methods)
        {
            var cmd = m.GetCustomAttribute<DebugCommandAttribute>();
            if (cmd is null) continue;
            if (m.ContainsGenericParameters) continue;
            _commands[cmd.Name] = CompileInvoker(m);
        }

        // properties
        var props = t.GetProperties(flags);
        foreach (var p in props)
        {
            var w = p.GetCustomAttribute<DebugWatchAttribute>();
            if (w is null) continue;
            if (p.GetMethod is null) continue; // must be readable

            var key = w.Name ?? p.Name;
            var rec = DebugWatchRecord.FromProperty(p, w.ReadOnly);
            _watches[key] = rec;
        }

        // fields
        var fields = t.GetFields(flags);
        foreach (var f in fields)
        {
            var w = f.GetCustomAttribute<DebugWatchAttribute>();
            if (w is null) continue;

            var key = w.Name ?? f.Name;
            var rec = DebugWatchRecord.FromField(f, w.ReadOnly);
            _watches[key] = rec;
        }
    }

    private static Func<object?, object?[], object?> CompileInvoker(MethodInfo mi)
    {
        var targetParam = Expression.Parameter(typeof(object), "target");
        var argsParam = Expression.Parameter(typeof(object[]), "args");

        var parms = mi.GetParameters();
        var argExpr = new Expression[parms.Length];
        for (var i = 0; i < parms.Length; i++)
        {
            var idx = Expression.ArrayIndex(argsParam, Expression.Constant(i));
            var cast = Expression.Convert(idx, parms[i].ParameterType);
            argExpr[i] = cast;
        }

        Expression call;
        if (mi.IsStatic)
        {
            call = Expression.Call(mi, argExpr);
        }
        else
        {
            var inst = Expression.Convert(targetParam, mi.DeclaringType!);
            call = Expression.Call(inst, mi, argExpr);
        }

        Expression body = mi.ReturnType == typeof(void)
            ? Expression.Block(call, Expression.Constant(null, typeof(object)))
            : Expression.Convert(call, typeof(object));

        return Expression.Lambda<Func<object?, object?[], object?>>(body, targetParam, argsParam).Compile();
    }

    private sealed class DebugWatchRecord
    {
        private readonly Func<object?, object?> _reader; // target -> value (target null for static)
        private readonly Action<object?, object?>? _writer; // target, value (null if read-only)
        private readonly bool _isStatic;

        private DebugWatchRecord(Func<object?, object?> reader, Action<object?, object?>? writer, bool isStatic)
        {
            _reader = reader;
            _writer = writer;
            _isStatic = isStatic;
        }

        public object? Read(object? target)
        {
            return _reader(_isStatic ? null : target);
        }

        public void Write(object? target, object? value)
        {
            if (_writer is null) throw new InvalidOperationException("Watch is read-only.");
            _writer(_isStatic ? null : target, value);
        }

        public static DebugWatchRecord FromProperty(PropertyInfo p, bool readOnly)
        {
            var get = p.GetMethod!;
            var set = p.SetMethod;

            var isStatic = get.IsStatic;
            var targetParam = Expression.Parameter(typeof(object), "target");

            // reader
            Expression instance = isStatic
                ? null!
                : Expression.Convert(targetParam, p.DeclaringType!);

            Expression getCall = isStatic
                ? Expression.Call(get)
                : Expression.Call(instance, get);

            var readerBody = Expression.Convert(getCall, typeof(object));
            var reader = Expression
                .Lambda<Func<object?, object?>>(readerBody, targetParam)
                .Compile();

            // writer
            Action<object?, object?>? writer = null;
            if (!readOnly && set is not null)
            {
                var valueParam = Expression.Parameter(typeof(object), "value");
                var valueCast = Expression.Convert(valueParam, p.PropertyType);

                Expression setCall = set.IsStatic
                    ? Expression.Call(set, valueCast)
                    : Expression.Call(Expression.Convert(targetParam, p.DeclaringType!), set, valueCast);

                writer = Expression
                    .Lambda<Action<object?, object?>>(setCall, targetParam, valueParam)
                    .Compile();
            }

            return new DebugWatchRecord(reader, writer, isStatic);
        }

        public static DebugWatchRecord FromField(FieldInfo f, bool readOnly)
        {
            var isStatic = f.IsStatic;
            var targetParam = Expression.Parameter(typeof(object), "target");

            // reader
            Expression field;
            if (isStatic)
            {
                field = Expression.Field(null, f);
            }
            else
            {
                var inst = Expression.Convert(targetParam, f.DeclaringType!);
                field = Expression.Field(inst, f);
            }

            var readerBody = Expression.Convert(field, typeof(object));
            var reader = Expression
                .Lambda<Func<object?, object?>>(readerBody, targetParam)
                .Compile();

            // writer
            Action<object?, object?>? writer = null;
            if (!readOnly && !f.IsInitOnly)
            {
                var valueParam = Expression.Parameter(typeof(object), "value");
                var valueCast = Expression.Convert(valueParam, f.FieldType);

                var assign = Expression.Assign(field, valueCast);
                writer = Expression
                    .Lambda<Action<object?, object?>>(assign, targetParam, valueParam)
                    .Compile();
            }

            return new DebugWatchRecord(reader, writer, isStatic);
        }
    }
}