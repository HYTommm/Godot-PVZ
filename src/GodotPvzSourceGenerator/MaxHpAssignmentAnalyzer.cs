using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace GodotPvzSourceGenerator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MaxHpAssignmentAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "HP001";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        "MaxHP 常量赋值过小",
        "对 '{0}' 赋值为 {1}，可能过小（建议 ≥3），否则默认阶段阈值可能重复",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "当 MaxHP 被赋值为小于等于 2 的常量时, Refresh 方法生成的默认阶段阈值MaxHP/3 和 2*MaxHP/3可能相同.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);

        // 注册两种构造函数初始化器语法类型
        context.RegisterSyntaxNodeAction(AnalyzeConstructorInitializer, SyntaxKind.BaseConstructorInitializer);
        context.RegisterSyntaxNodeAction(AnalyzeConstructorInitializer, SyntaxKind.ThisConstructorInitializer);
    }

    private void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
    {
        var assignment = (AssignmentExpressionSyntax)context.Node;
        var leftSymbol = context.SemanticModel.GetSymbolInfo(assignment.Left).Symbol;

        //if (leftSymbol is IPropertySymbol or IFieldSymbol { Name: "MaxHP" })
        //{
        //    var typeName = leftSymbol.ContainingType.ToDisplayString();
        //    var diagnostic = Diagnostic.Create(
        //        new DiagnosticDescriptor("HP998", "Debug", $"ContainingType: '{typeName}'", "Debug", DiagnosticSeverity.Warning, true),
        //        context.Node.GetLocation());
        //    context.ReportDiagnostic(diagnostic);
        //}

        if (IsMaxHpProperty(leftSymbol))
        {
            if (leftSymbol?.Name != null) CheckConstantValue(context, assignment.Right, leftSymbol.Name);
        }
    }

    private void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var creation = (ObjectCreationExpressionSyntax)context.Node;
        var constructorSymbol = context.SemanticModel.GetSymbolInfo(creation).Symbol as IMethodSymbol;

        if (constructorSymbol == null) return;
        if (!IsHealthStageComponentType(constructorSymbol.ContainingType)) return;

        // 遍历构造函数参数，找到名为 maxHP 的参数
        for (int i = 0; i < constructorSymbol.Parameters.Length; i++)
        {
            var parameter = constructorSymbol.Parameters[i];
            if (parameter.Name.Equals("maxHP", System.StringComparison.OrdinalIgnoreCase))
            {
                // 获取对应位置传入的表达式
                if (creation.ArgumentList?.Arguments.Count > i)
                {
                    var argExpression = creation.ArgumentList.Arguments[i].Expression;
                    CheckConstantValue(context, argExpression, "MaxHP");
                }
            }
        }
    }

    private void AnalyzeConstructorInitializer(SyntaxNodeAnalysisContext context)
    {
        var initializer = (ConstructorInitializerSyntax)context.Node;
        var constructorSymbol = context.SemanticModel.GetSymbolInfo(initializer).Symbol as IMethodSymbol;

        if (constructorSymbol == null) return;
        if (!IsHealthStageComponentType(constructorSymbol.ContainingType)) return;

        // 同样检查参数列表中名为 maxHP 的参数
        for (int i = 0; i < constructorSymbol.Parameters.Length; i++)
        {
            var parameter = constructorSymbol.Parameters[i];
            if (parameter.Name.Equals("maxHP", System.StringComparison.OrdinalIgnoreCase))
            {
                if (initializer.ArgumentList?.Arguments.Count > i)
                {
                    var argExpression = initializer.ArgumentList.Arguments[i].Expression;
                    CheckConstantValue(context, argExpression, "MaxHP");
                }
            }
        }
    }

    private void CheckConstantValue(SyntaxNodeAnalysisContext context, ExpressionSyntax expression, string propertyName)
    {
        var constantValue = context.SemanticModel.GetConstantValue(expression);
        if (constantValue.HasValue && constantValue.Value is int intValue && intValue <= 2)
        {
            var diagnostic = Diagnostic.Create(Rule, expression.GetLocation(), propertyName, intValue);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private bool IsMaxHpProperty(ISymbol? symbol)
    {
        return symbol is IPropertySymbol or IFieldSymbol && symbol.Name == "MaxHP" &&
               IsHealthStageComponentType(symbol.ContainingType);
    }

    private bool IsHealthStageComponentType(INamedTypeSymbol type) =>
        type?.ToDisplayString() == "HealthStageComponent"; // 替换为实际完全限定名
}