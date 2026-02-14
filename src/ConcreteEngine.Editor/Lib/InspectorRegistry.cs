using System.Collections;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Editor.Lib;

public static class InspectorRegistry
{
    private static readonly Dictionary<Type, InspectorFieldMeta[]> TypeCache = new(16);

    public static bool TryGet(Type type, out InspectorFieldMeta[] entries) => TypeCache.TryGetValue(type, out entries!);

    public static void RegisterType(Type type)
    {
        if (!type.IsClass && !type.IsValueType) throw new ArgumentException(type.Name, nameof(type));
        TypeCache.TryAdd(type, BuildTypeMetadata2(type));
    }

    private static void RegisterPrimitiveStruct(Type type)
    {
        if (!type.IsValueType) throw new ArgumentException(type.Name, nameof(type));
        TypeCache.TryAdd(type, BuildPrimitiveStructMetadata(type));
    }

    private static InspectorFieldMeta[] BuildTypeMetadata2(Type type)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

        var typeAttr = type.GetCustomAttribute<InspectableAttribute>();
        var primitiveAttr = type.GetCustomAttribute<InspectablePrimitiveAttribute>();
        var hasTypeAttr = typeAttr != null || primitiveAttr != null;

        var typeFormat = typeAttr?.Format;

        var list = new List<InspectorFieldMeta>(4);
        
        var members = type.GetMembers(flags);
        foreach (var member in members)
        {
            if ((member.MemberType & (MemberTypes.Property | MemberTypes.Field)) == 0)
                continue;

            var attr = member.GetCustomAttribute<InspectableAttribute>();
            var primitiveTypeAttr = member.GetCustomAttribute<InspectablePrimitiveAttribute>();

            if (attr == null && primitiveTypeAttr == null && !hasTypeAttr) continue;

            Func<object, object> getter;
            Type memberType;
            

            switch (member)
            {
                case PropertyInfo { CanRead: true } prop:
                    memberType = prop.PropertyType;
                    getter = CreatePropertyGetter(prop);
                    break;
                case FieldInfo field:
                    memberType = field.FieldType;
                    getter = CreateFieldGetter(field);
                    break;
                default: continue;
            }

            var format = attr?.Format ?? typeFormat;
            var kind = GetKind(memberType, primitiveAttr != null || primitiveTypeAttr != null);
            list.Add(new InspectorFieldMeta(kind, memberType, member.Name, format, getter));

            if (primitiveTypeAttr != null && !TypeCache.ContainsKey(memberType))
                RegisterPrimitiveStruct(memberType);
        }

        return list.ToArray();
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static InspectorFieldMeta[] BuildPrimitiveStructMetadata(Type type)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
        var list = new List<InspectorFieldMeta>(2);

        var fields = type.GetFields(flags);
        foreach (var it in fields)
        {
            if (!it.FieldType.IsPrimitive) continue;

            var getter = CreateFieldGetter(it);
            list.Add(new InspectorFieldMeta(InspectorTypeKind.Primitive, it.FieldType, it.Name, null, getter));
        }

        var props = type.GetProperties(flags);
        foreach (var it in props)
        {
            if (!it.PropertyType.IsPrimitive || !it.CanRead) continue;

            var getter = CreatePropertyGetter(it);
            list.Add(new InspectorFieldMeta(InspectorTypeKind.Primitive, it.PropertyType, it.Name, null, getter));
        }

        return list.ToArray();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Func<object, object> CreateFieldGetter(FieldInfo field)
    {
        var instanceParam = Expression.Parameter(typeof(object), "obj");

        var typedInstance = Expression.Convert(instanceParam, field.DeclaringType!);
        var fieldAccess = Expression.Field(typedInstance, field);

        var convertResult = Expression.Convert(fieldAccess, typeof(object));

        var lambda = Expression.Lambda<Func<object, object>>(convertResult, instanceParam);

        return lambda.Compile();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Func<object, object> CreatePropertyGetter(PropertyInfo prop)
    {
        var instanceParam = Expression.Parameter(typeof(object), "obj");

        var typedInstance = Expression.Convert(instanceParam, prop.DeclaringType!);
        var propertyAccess = Expression.Property(typedInstance, prop);

        var convertResult = Expression.Convert(propertyAccess, typeof(object));

        var lambda = Expression.Lambda<Func<object, object>>(convertResult, instanceParam);

        return lambda.Compile();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static InspectorTypeKind GetKind(Type type, bool primitiveAttrib)
    {
        if (type.IsPrimitive || type == typeof(Guid) || type == typeof(DateTime))
            return InspectorTypeKind.Primitive;
        if (type == typeof(string))
            return InspectorTypeKind.String;
        if (primitiveAttrib)
            return InspectorTypeKind.PrimitiveStruct;
        if (type.IsValueType)
            return InspectorTypeKind.Struct;
        if (typeof(IDictionary).IsAssignableFrom(type))
            return InspectorTypeKind.Map;
        if (typeof(IList).IsAssignableFrom(type))
            return InspectorTypeKind.Array;
        if (type.IsClass)
            return InspectorTypeKind.Class;

        return InspectorTypeKind.Unknown;
    }
}
/*

    private static InspectorFieldMeta[] BuildTypeMetadata(Type type)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

        var typeAttr = type.GetCustomAttribute<InspectableAttribute>();
        var primitiveAttr = type.GetCustomAttribute<InspectablePrimitiveAttribute>();
        var hasTypeAttr = typeAttr != null || primitiveAttr != null;

        var typeFormat = typeAttr?.Format;

        var fields = type.GetFields(flags);

        var list = new List<InspectorFieldMeta>(4);

        foreach (var field in fields)
        {
            var attr = field.GetCustomAttribute<InspectableAttribute>();
            var primitiveFieldAttr = field.GetCustomAttribute<InspectablePrimitiveAttribute>();

            if (attr == null && primitiveFieldAttr == null && !hasTypeAttr) continue;
            var fieldType = field.FieldType;

            if (primitiveFieldAttr != null && !TypeCache.ContainsKey(fieldType))
                RegisterPrimitiveStruct(fieldType);

            var format = attr?.Format ?? typeFormat;
            var kind = GetKind(fieldType, primitiveAttr != null || primitiveFieldAttr != null);
            var getter = CreateFieldGetter(field);
            list.Add(new InspectorFieldMeta(kind, fieldType, field.Name, format, getter));
        }

        var props = type.GetProperties(flags);
        foreach (var prop in props)
        {
            if (!prop.CanRead) continue;
            if (prop.GetIndexParameters().Length > 0) continue;

            var attr = prop.GetCustomAttribute<InspectableAttribute>();
            var primitivePropAttr = prop.GetCustomAttribute<InspectablePrimitiveAttribute>();

            if (attr == null && primitivePropAttr == null && !hasTypeAttr) continue;
            var propType = prop.PropertyType;

            if (primitivePropAttr != null && !TypeCache.ContainsKey(propType))
                RegisterPrimitiveStruct(propType);

            var format = attr?.Format ?? typeFormat;
            var kind = GetKind(propType, primitiveAttr != null || primitivePropAttr != null);
            var getter = CreatePropertyGetter(prop);

            list.Add(new InspectorFieldMeta(kind, propType, prop.Name, format, getter));
        }

        return list.ToArray();
    }
*/