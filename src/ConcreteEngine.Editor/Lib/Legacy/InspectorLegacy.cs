namespace ConcreteEngine.Editor.Lib.Legacy;


/*

   public sealed class InspectorObject(string objectName)
   {
       public string InspectorName = "";
       public string ObjectName = objectName;

       public int IntId = -1;
       public long Gen = -1;
       public Guid GId = Guid.Empty;

       public readonly List<InspectorBaseItem> Items = new(8);
       public readonly List<InspectorBaseItem> Pending = [];

       public InspectorItem AddItem(string name, string info = "")
       {
           var item = new InspectorItem(name, info);
           Items.Add(item);
           return item;
       }

       public T Add<T>(T item) where T : InspectorBaseItem
       {
           Items.Add(item);
           return item;
       }

       public void EndFrame()
       {
           if (Pending.Count == 0) return;

           Items.AddRange(Pending);
           Pending.Clear();
       }
   }

   public abstract class InspectorBaseItem(string fieldName, string info)
   {
       public readonly string FieldName = fieldName;
       public readonly string Info = info; //

       public abstract Span<UiTextProperty> GetProperties();
       internal abstract void Draw(FrameContext ctx);
   }

   public sealed class InspectorItem(string fieldName, string info) : InspectorBaseItem(fieldName, info)
   {
       public readonly string FieldName = fieldName;
       public readonly string Info = info; //

       public List<UiTextProperty> Fields = [];
       public override Span<UiTextProperty> GetProperties() => CollectionsMarshal.AsSpan(Fields);

       internal override void Draw(FrameContext ctx)
       {
           foreach (ref var it in CollectionsMarshal.AsSpan(Fields))
           {
               ImGui.TextUnformatted(ref it.Label.GetRef());
               ImGui.SameLine();
               ImGui.TextUnformatted(ref it.Value.GetRef());
           }
       }
   }

   public sealed class InspectorArrayItem(
       string fieldName,
       string info,
       Type elementType,
       int length
   ) : InspectorBaseItem(fieldName, info)
   {
       public readonly string FieldName = fieldName;
       public readonly string Info = info; //

       public Type ElementType = elementType;

       public List<UiTextProperty> Fields = new(length);

       public override Span<UiTextProperty> GetProperties() => CollectionsMarshal.AsSpan(Fields);

       internal override void Draw(FrameContext ctx)
       {
           var sw = ctx.Writer;
           var id = 0;
           foreach (ref var it in CollectionsMarshal.AsSpan(Fields))
           {
               ImGui.PushID(id++);
               if (ImGui.TreeNodeEx(ref sw.Write(FieldName), ImGuiTreeNodeFlags.SpanFullWidth))
               {
                   ImGui.TextUnformatted(ref it.Label.GetRef());
                   ImGui.SameLine();
                   ImGui.TextUnformatted(ref it.Value.GetRef());

                   ImGui.TreePop();
               }

               ImGui.PopID();
           }
       }
   }


   private static void BuildFlat(in BuilderContext ctx, InspectorItem item, InspectorFieldMeta meta, object? target)
   {
       if (target == null) return;

       if (meta.TypeKind == InspectorTypeKind.Unknown)
       {
           item.Fields.Add(UiTextProperty.Make(meta.Name, "Unknown", 0));
           return;
       }

       if (meta.TypeKind is InspectorTypeKind.Primitive or InspectorTypeKind.String)
       {
           var value = FormatValue(target, default, ctx.Sw);
           item.Fields.Add(UiTextProperty.Make(meta.Name, value, 0));
           return;
       }

       if (meta.TypeKind == InspectorTypeKind.PrimitiveStruct)
       {
           if (!InspectorRegistry.TryGet(meta.Type, out var entries)) return;
           var halfCap = ctx.Sw.Capacity / 2;

           var itemSw = ctx.Sw.GetSlicedWriter(0, halfCap);
           var formatSw = ctx.Sw.GetSlicedWriter(halfCap, halfCap * 2);

           itemSw.Start('[');
           for (var i = 0; i < entries.Length; i++)
           {
               var it = entries[i];
               var value = it.Getter(target);
               var valueUtf8 = FormatValue(value, new FormatOptions(it.Format, it.TypeKind), formatSw);
               itemSw.Append(valueUtf8);
               if (i < entries.Length - 1) itemSw.Append(',');
           }

           item.Fields.Add(UiTextProperty.Make(meta.Name, itemSw.Append(']').EndSpan(), 0));
           return;
       }

       if (meta.TypeKind == InspectorTypeKind.Struct)
       {
           if (!InspectorRegistry.TryGet(meta.Type, out var entries)) return;
           item.Fields.Add(UiTextProperty.Make(meta.Name, ReadOnlySpan<byte>.Empty, 0));
           var newItem = ctx.Inspector.AddItem(meta.Name);
           foreach (var it in entries)
           {
               var value = it.Getter(target);
               var valueUtf8 = FormatValue(value, new FormatOptions(it.Format, it.TypeKind), ctx.Sw);
               newItem.Fields.Add(UiTextProperty.Make(it.Name, valueUtf8, 1));
           }

           return;
       }

       if (meta.TypeKind == InspectorTypeKind.Class)
       {
           ctx.Inspector.AddItem(meta.Name);
           return;
       }

       if (meta.TypeKind == InspectorTypeKind.Array)
       {
           var list = (IList)target;
           if (list.Count == 0) return;
           var type = list[0]!.GetType();
           var arrayItem = ctx.Inspector.Add(new InspectorArrayItem(meta.Name, "", type, list.Count));

           int idx = 0;
           foreach (var it in list)
           {
               InspectorRegistry.TryGet(it.GetType(), out var entries);

               foreach (var entry in entries)
               {
                   var value = entry.Getter(it);
                   var valueUtf8 = FormatValue(value, new FormatOptions(entry.Format, entry.TypeKind), ctx.Sw);
                   arrayItem.Fields.Add(UiTextProperty.Make($"[{idx++}]", valueUtf8, 1));
               }
           }

           return;
       }

       if (meta.TypeKind == InspectorTypeKind.Map)
       {
           var dict = (IDictionary)target;
           ctx.Inspector.AddItem(meta.Name, dict.Count.ToString());
       }
   }
   private static FieldEntry[] BuildTypeMetadata(Type type)
   {
       const BindingFlags flags =
           BindingFlags.Instance |
           BindingFlags.Public;

       var typeAttr = type.GetCustomAttribute<InspectableAttribute>();
       var typeFormat = typeAttr?.Format;

       var list = new List<FieldEntry>(8);

       var fields = type.GetFields(flags);
       foreach (var field in fields)
       {
           var attr = field.GetCustomAttribute<InspectableAttribute>();
           if (attr == null && typeAttr == null) continue;

           var format = attr?.Format ?? typeFormat;

           var getter = CreateFieldGetter(field);
           list.Add(new FieldEntry(field.Name, getter, format));
       }

       var props = type.GetProperties(flags);
       foreach (var prop in props)
       {
           if (!prop.CanRead) continue;
           if (prop.GetIndexParameters().Length > 0) continue;

           var attr = prop.GetCustomAttribute<InspectableAttribute>();
           if (attr == null && typeAttr == null)
               continue;

           var format = attr?.Format ?? typeFormat;

           var getter = CreatePropertyGetter(prop);
           list.Add(new FieldEntry(prop.Name, getter, format));
       }

       return list.ToArray();
   }


   public static class InspectorBuilder
   {
       internal static UnsafeSpanWriter WriterUtf8;

       public static void Build(Type type, object target, List<Row> rows)
       {
           rows.Clear();

           var entries = InspectorRegistry.Get(type);
           foreach (var entry in entries)
           {
               var value = entry.Getter(target);
               var valueCompiled = FormatValue(value, new FormatOptions { Format = entry.Format });
               rows.Add(Row.Make(entry.Label, valueCompiled));
           }
       }

       private static ReadOnlySpan<byte> FormatValue(object target, in FormatOptions format)
       {
           switch (target)
           {
               case byte b: WriterUtf8.Start((int)b); break;
               case short s: WriterUtf8.Start((int)s); break;
               case ushort us: WriterUtf8.Start((int)us); break;
               case int i: WriterUtf8.Start(i); break;
               case uint ui: WriterUtf8.Start(ui); break;
               case float f: WriterUtf8.Start(f, format.Format); break;
               case double d: WriterUtf8.Start(d, format.Format); break;
               case long l: WriterUtf8.Start(l, format.Format); break;
               case ulong ul: WriterUtf8.Start(ul, format.Format); break;
               case Guid g: WriterUtf8.Start(g, format.Format); break;
               case DateTime dt: WriterUtf8.Start(dt, format.Format); break;
               case string s: WriterUtf8.Start(s); break;
               case bool bo: WriterUtf8.Start(bo ? "true" : "false"); break;
               default: WriterUtf8.Start(target?.ToString() ?? "null"); break;
           }

           return WriterUtf8.EndSpan();
       }
   }



 */


/*
public static class InspectorMetadata<T>
{
    public static readonly FieldEntry<T>[] Fields = BuildMetadata();

    private static FieldEntry<T>[] BuildMetadata()
    {
        var type = typeof(T);

        var fields = type.GetFields(
            BindingFlags.Instance |
            BindingFlags.Public);

        var list = new List<FieldEntry<T>>(fields.Length);

        foreach (var field in fields)
        {
            var getter = CreateGetter(field);

            var formatAttr = field.GetCustomAttribute<InspectorFormatAttribute>();
            var format = formatAttr?.Format;

            list.Add(new FieldEntry<T>(field.Name, getter, format));
        }

        return list.ToArray();
    }

    private static FieldFormatter<T> CreateGetter(FieldInfo field)
    {
        var targetParam = Expression.Parameter(typeof(T).MakeByRefType(), "target");
        var formatParam = Expression.Parameter(typeof(FormatOptions).MakeByRefType(), "format");

        var fieldAccess = Expression.Field(targetParam, field);

        var method = typeof(InspectorBuilder)
            .GetMethod(nameof(InspectorBuilder.FormatValue))
            !.MakeGenericMethod(field.FieldType);

        var call = Expression.Call(method, fieldAccess, formatParam);

        var lambda = Expression.Lambda<FieldFormatter<T>>(call, targetParam, formatParam);

        return lambda.Compile();
    }
}

public static class InspectorBuilder
{
    public static unsafe void Build<T>(ref T target, List<Row> rows)
    {
        rows.Clear();
        foreach (var entry in InspectorMetadata<T>.Fields)
        {
            entry.Getter(ref target, new FormatOptions { Format = entry.Format });

            rows.Add(Row.Make(entry.Label, sw.EndSpan()));
        }
    }

    internal static void FormatValue<T>(ref T target, in FormatOptions format)
    {
        var sw = EditorService.GetWriter();
        switch (target)
        {
            case int i: sw.Start(i); break;
            case float f: sw.Start(f, format.Format); break;
            case Guid g: sw.Start(g, format.Format); break;
            case string s: sw.Start(s); break;
            default: sw.Start(target?.ToString() ?? "null"); break;
        }
    }
}
public delegate void FieldFormatter<T>(ref T target, in FormatOptions format);
public sealed class FieldEntry<T>
{
    public readonly string Label;
    public readonly string? Format;
    public readonly FieldFormatter<T> Getter;

    public FieldEntry(string label, FieldFormatter<T> getter, string? format)
    {
        Label = label;
        Getter = getter;
        Format = format;
    }
}*/