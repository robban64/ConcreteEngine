using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Generator.InspectorGen;

[Generator]
public sealed partial class InspectorGenerator : IIncrementalGenerator
{
    private const string MainAttributeFullName = "ConcreteEngine.Editor.Lib.EditorInspectorAttribute";
    private const string MainAttribute = "EditorInspectorAttribute";

    private const string InspectAttrib = "InspectAttribute";
    private const string IncludeAttrib = "InspectIncludeAttribute";
    private const string InputNumberAttrib = "InputNumberAttribute";
    private const string InputColorAttrib = "InputColorAttribute";
    private const string InputComboAttrib = "InputComboAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var valueProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(MainAttributeFullName, Predicates.IsObjectOrStruct, Build)
            .Where(static x => x != null!);

        context.RegisterSourceOutput(valueProvider, static (ctx, value) =>
        {
            var source = Emit(value);
            ctx.AddSource($"{value.TargetName}Inspector.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }


    private static InspectModel Build(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var inspectorSym = (INamedTypeSymbol)ctx.TargetSymbol;
        var inspectorAttr = inspectorSym.GetAttributes().First(x => x.AttributeClass?.Name == MainAttribute);

        var targetSym =
            (INamedTypeSymbol)inspectorAttr.ConstructorArguments.First(x => x.Kind == TypedConstantKind.Type).Value!;
        var targetAttr = targetSym.GetAttributes().First(x => x.AttributeClass?.Name == InspectAttrib);
        //

        string? displayName = null;
        foreach (var (key, constant) in targetAttr.NamedArguments)
        {
            switch (key, constant.Value)
            {
                case ("DisplayName", string s): displayName = s; break;
            }
        }

        var members = GenerateMemberFor(targetSym);
        
        return new InspectModel(
            InspectorName: inspectorSym.Name,
            InspectorNs: inspectorSym.ContainingNamespace.ToDisplayString(),
            TargetName: targetSym.Name,
            TargetNs: targetSym.ContainingNamespace.ToDisplayString(),
            Members: members
        ) { DisplayName = displayName };
    }

    private static EquatableArray<TargetMemberInfo> GenerateMemberFor(INamedTypeSymbol targetSym)
    {
        var list = new List<TargetMemberInfo>(8);

        var members = targetSym.GetMembers().Where(MemberFilter);
        foreach (var member in members)
        {
            if (CreateMember(member, null, null) is { } created)
            {
                list.Add(created);
                continue;
            }

            var includeAttr = member.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name is IncludeAttrib);
            if (includeAttr is not null)
            {
                var includeAttrCtor = includeAttr.ConstructorArguments;

                var nestedAccessPath = member.Name;
                if (!includeAttrCtor.IsEmpty && includeAttrCtor[0].Value is string accessSuffix)
                    nestedAccessPath += $".{accessSuffix}";

                var info = MemberInfo.Extract(member);
                var includeTypeSym = member.GetFieldOrPropertyType();
                foreach (var nestedMember in includeTypeSym.GetMembers().Where(MemberFilter))
                {
                    if (CreateMember(nestedMember, info, nestedAccessPath) is { } createdInner)
                        list.Add(createdInner);
                }
            }
        }

        return list.ToEquatableArray();


        static TargetMemberInfo? CreateMember(ISymbol sym, MemberInfo? parentInfo, string? nestedAccessPath)
        {
            var inputAttr = sym.GetAttributes().FirstOrDefault(static x =>
                x.AttributeClass?.Name is InputNumberAttrib or InputColorAttrib or InputComboAttrib);

            if (inputAttr == null || inputAttr.AttributeClass is null) return null;

            var typeSym = sym.GetFieldOrPropertyType();

            var label = sym.Name;
            var ctor = inputAttr.ConstructorArguments;
            if (ctor.Length > 0 && ctor[0].Value is string displayName)
                label = displayName;

            InputField? inputField = inputAttr.AttributeClass.Name switch
            {
                InputNumberAttrib => MakeInputField(sym.Name, label, inputAttr, typeSym),
                InputColorAttrib => MakeColorField(sym.Name, label, inputAttr, typeSym),
                InputComboAttrib => MakeComboField(sym.Name, label, inputAttr, typeSym),
                _ => throw new UnreachableException()
            };
            if (inputField is null) return null;

            var ns = sym.ContainingNamespace.ToDisplayString();
            var info = MemberInfo.Extract(sym);
            return new TargetMemberInfo(sym.Name, ns, typeSym.ToDisplayString(), info)
            {
                //Segment = parentTypeSym?.Name,
                Input = inputField, IncludeName = nestedAccessPath, ParentInfo = parentInfo
            };
        }
    }
}

/*
 private static EquatableArray<TargetModel> Transformer(Compilation compilation, CancellationToken ct)
      {
          ct.ThrowIfCancellationRequested();
          var coreAssembly = compilation.SourceModule
              .ReferencedAssemblySymbols.FirstOrDefault(a => a.Name.EndsWith("Core.Engine"));

          if (coreAssembly is null) return [];

          var list = new List<TargetModel>();
          foreach (var type in GetAllTypes(coreAssembly.GlobalNamespace))
          {
              if (!type.IsPublicClassOrStruct()) continue;
              var attr = type.GetAttributes().FirstOrDefault(static it => it.AttributeClass?.Name == InspectAttrib);
              if (attr is not null && Generate(type, attr) is { } targetModel)
                  list.Add(targetModel);
          }

          return list.ToImmutableArray().AsEquatableArray();
      }
*/