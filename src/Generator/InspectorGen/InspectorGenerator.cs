using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Generator.InspectorGen;

[Generator]
public sealed partial class InspectorGenerator : IIncrementalGenerator
{
    private const string MainAttribute = "EditorInspectorAttribute";
    private const string MainAttributeFullName = "ConcreteEngine.Editor.Lib.EditorInspectorAttribute";

    private const string InspectAttrib = "InspectAttribute";
    private const string IncludeAttrib = "InspectIncludeAttribute";
    private const string InputNumberAttrib = "InputNumberAttribute";
    private const string InputColorAttrib = "InputColorAttribute";
    private const string InputComboAttrib = "InputComboAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var valueProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(MainAttributeFullName, Predicates.IsClassNode, Build)
            .Where(static x => x != null!);

        context.RegisterSourceOutput(valueProvider, static (ctx, value) =>
        {
            var source = InspectorGeneratorEmitter.Emit(value);
            ctx.AddSource($"{value.TargetName}Inspector.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }


    private static InspectModel Build(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var inspectorSym = (INamedTypeSymbol)ctx.TargetSymbol;
        var inspectorAttr = inspectorSym.GetAttributes().First(static x => x.AttributeClass?.Name == MainAttribute);

        var targetSym = (INamedTypeSymbol)inspectorAttr.ConstructorArguments
            .First(static x => x.Kind == TypedConstantKind.Type).Value!;
        var targetAttr = targetSym.GetAttributes().First(static x => x.AttributeClass?.Name == InspectAttrib);
        //

        string? displayName = null;
        foreach (var (key, constant) in targetAttr.NamedArguments)
        {
            switch (key, constant.Value)
            {
                case ("DisplayName", string s): displayName = s; break;
            }
        }

        GenerateMemberFor(targetSym, out var groups);

        return new InspectModel(
            InspectorName: inspectorSym.Name,
            InspectorNs: inspectorSym.ContainingNamespace.ToDisplayString(),
            TargetName: targetSym.Name,
            TargetNs: targetSym.ContainingNamespace.ToDisplayString(),
            Groups: groups
        ) { DisplayName = displayName };
    }

    private static void GenerateMemberFor(INamedTypeSymbol targetSym, out EquatableArray<InspectorGroup> groupArray)
    {
        var roots = new List<InspectorMember>(4);
        var groups = new List<InspectorGroup>(4);

        foreach (var member in targetSym.GetMembers().Where(MemberFilter))
        {
            if (CreateMember(member) is { } created)
            {
                roots.Add(created);
                continue;
            }

            if (TryParseIncludeAttribute(member, out string? nestedName))
            {
                var accessPath = member.Name;
                if (nestedName is not null) accessPath += "." + nestedName;
                var includeType = member.GetFieldOrPropertyType();

                var groupMembers = new List<InspectorMember>();
                foreach (var nestedMember in includeType.GetMembers().Where(MemberFilter))
                {
                    if (CreateMember(nestedMember) is { } createdInner) 
                        groupMembers.Add(createdInner);
                }

                if (groupMembers.Count > 0)
                {
                    groups.Add(new InspectorGroup(false,
                        Name: member.Name,
                        AccessPath: accessPath,
                        Info: MemberInfo.Extract(member),
                        Members: groupMembers.ToEquatableArray()));
                }
            }
        }

        if (roots.Count > 0)
        {
            groups.Insert(0, new InspectorGroup(true,
                Name: "Root",
                AccessPath: "",
                Info: default,
                Members: roots.ToEquatableArray()));
        }

        groupArray = groups.Count > 0 ? groups.ToEquatableArray() : [];
        return;

        static InspectorMember? CreateMember(ISymbol sym)
        {
            var attr = sym.GetAttributes().FirstOrDefault(static x =>
                x.AttributeClass?.Name is InputNumberAttrib or InputColorAttrib or InputComboAttrib);

            if (attr is null || attr.AttributeClass is null) return null;

            var typeSym = sym.GetFieldOrPropertyType();
            InputField? inputField = attr.AttributeClass!.Name switch
            {
                InputNumberAttrib => MakeInputField(sym.Name, attr, typeSym),
                InputColorAttrib => MakeColorField(sym.Name, attr, typeSym),
                InputComboAttrib => MakeComboField(sym.Name, attr, typeSym),
                _ => null
            };
            if (inputField is null) return null;

            ExtractCommonFieldAttr(attr, out var displayName, out var segment);
            
            return new InspectorMember(Name: sym.Name,
                DisplayName: displayName ?? sym.Name,
                TargetNs: sym.ContainingNamespace.ToDisplayString(),
                TypeName: typeSym.ToDisplayString(),
                Info: MemberInfo.Extract(sym)) { Segment = segment, Input = inputField };
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