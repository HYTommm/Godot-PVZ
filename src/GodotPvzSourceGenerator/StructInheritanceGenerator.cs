using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// StructInheritanceAttribute.cs
namespace StructInheritance
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
    public sealed class StructInheritanceAttribute(Type sourceType) : Attribute
    {
        public Type SourceType { get; } = sourceType;
        public bool IncludeNonPublic { get; set; } // 可选扩展
    }
}



namespace StructInheritance.Generator
{
    [Microsoft.CodeAnalysis.Generator]
    public class StructInheritanceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {

            // 1. 生成特性类的源代码
            context.RegisterPostInitializationOutput(ctx =>
            {
                string attributeSource = @"
using System;

namespace StructInheritance
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
    public sealed class StructInheritanceAttribute : Attribute
    {
        public Type SourceType { get; }
        public bool IncludeNonPublic { get; set; }
        public StructInheritanceAttribute(Type sourceType) => SourceType = sourceType;
    }
}";
                ctx.AddSource("StructInheritanceAttribute.g.cs", SourceText.From(attributeSource, Encoding.UTF8));
            });
            // 1. 获取所有带有 [StructInheritance] 特性的 partial struct 声明
            var structsWithAttr = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => node is StructDeclarationSyntax structDecl && structDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                    transform: static (ctx, _) => GetStructSymbol(ctx))
                .Where(symbol => symbol is not null)
                .Collect();

            // 2. 组合编译信息，确保能解析类型引用
            var combined = context.CompilationProvider.Combine(structsWithAttr);

            // 3. 注册生成逻辑
            context.RegisterSourceOutput(combined, (spc, tuple) =>
            {
                var (compilation, structSymbols) = tuple;
                foreach (var structSymbol in structSymbols)
                {
                    GenerateStructFields(spc, structSymbol!, compilation);
                }
            });
        }

        private static INamedTypeSymbol? GetStructSymbol(GeneratorSyntaxContext ctx)
        {
            var structDecl = (StructDeclarationSyntax)ctx.Node;
            var model = ctx.SemanticModel;
            return model.GetDeclaredSymbol(structDecl) as INamedTypeSymbol;
        }

        private static void GenerateStructFields(SourceProductionContext spc, INamedTypeSymbol structSymbol, Compilation compilation)
        {
            // 查找 [StructInheritance] 特性
            var attrType = compilation.GetTypeByMetadataName("StructInheritance.StructInheritanceAttribute");
            if (attrType == null) return;

            var attributes = structSymbol.GetAttributes()
                .Where(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attrType))
                .ToList();
            if (attributes.Count == 0) return;

            var allFieldDeclarations = new StringBuilder();
            var existingFieldNames = new HashSet<string>(
                structSymbol.GetMembers().OfType<IFieldSymbol>().Select(f => f.Name)
            );

            foreach (var attr in attributes)
            {
                if (attr.ConstructorArguments.Length == 0) continue;
                var sourceTypeArg = attr.ConstructorArguments[0];
                if (sourceTypeArg.Value is not INamedTypeSymbol sourceType) continue;

                // 获取源类型的所有实例字段（公共，非static）
                var fields = sourceType.GetMembers().OfType<IFieldSymbol>()
                    .Where(f => !f.IsStatic && f.DeclaredAccessibility == Accessibility.Public)
                    .ToList();

                // 可选：支持 IncludeNonPublic
                bool includeNonPublic = false;
                foreach (var namedArg in attr.NamedArguments)
                {
                    if (namedArg.Key == "IncludeNonPublic" && namedArg.Value.Value is bool b)
                        includeNonPublic = b;
                }
                if (includeNonPublic)
                {
                    fields = sourceType.GetMembers().OfType<IFieldSymbol>()
                        .Where(f => !f.IsStatic && (f.DeclaredAccessibility == Accessibility.Public || f.DeclaredAccessibility == Accessibility.Internal))
                        .ToList();
                }

                foreach (var field in fields)
                {
                    if (existingFieldNames.Contains(field.Name))
                    {
                        // 报告冲突错误
                        var diagnostic = Diagnostic.Create(
                            new DiagnosticDescriptor("SI001", "Field name conflict",
                                $"Field '{field.Name}' from '{sourceType.Name}' already exists in '{structSymbol.Name}'",
                                "StructInheritance", DiagnosticSeverity.Error, true),
                            Location.None);
                        spc.ReportDiagnostic(diagnostic);
                        continue;
                    }

                    // 生成字段声明：保留类型和名称
                    string fieldType = field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    string fieldDecl = $"public {fieldType} {field.Name};";
                    allFieldDeclarations.AppendLine(fieldDecl);
                    existingFieldNames.Add(field.Name); // 避免同一声明中重复
                }
            }

            if (allFieldDeclarations.Length == 0) return;

            // 生成 partial struct 的附加部分
            string? namespaceName = structSymbol.ContainingNamespace?.IsGlobalNamespace == false
                ? structSymbol.ContainingNamespace.ToDisplayString()
                : null;
            string structName = structSymbol.Name;



            string generatedCode;
            if (string.IsNullOrEmpty(namespaceName))
            {
                // 全局命名空间：不生成 namespace 包裹
                generatedCode = $$"""
                                  // <auto-generated/>
                                  partial struct {{structName}}
                                  {
                                      {{allFieldDeclarations}}
                                  }
                                  """;
            }
            else
            {
                generatedCode = $$"""
                                  // <auto-generated/>
                                  namespace {{namespaceName}}
                                  {
                                      partial struct {{structName}}
                                      {
                                          {{allFieldDeclarations}}
                                      }
                                  }
                                  """;
            }
            
            spc.AddSource($"{structName}.g.cs", SourceText.From(generatedCode, Encoding.UTF8));
        }
    }
}