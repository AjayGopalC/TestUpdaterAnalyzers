using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using NXunitConverterAnalyzer.Recognizers;
using System.Collections.Immutable;
using System.Linq;

namespace NXunitConverterAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NXunitConverterAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "NXunitConverterAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Unit Tests";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is MethodDeclarationSyntax methodSyntax && methodSyntax.AttributeLists.Any())
            {
                if (methodSyntax.Parent is ClassDeclarationSyntax classDeclaration
                    && ((classDeclaration.BaseList?.Types.Any() ?? false)
                       || (classDeclaration.Members.Any(x => x.IsKind(SyntaxKind.ConstructorDeclaration)))))
                {
                    return;
                }

                foreach (var attribute in methodSyntax.AttributeLists.SelectMany(x => x.Attributes))
                {
                    var attributeSymbol = context.SemanticModel.GetSymbolInfo(attribute).Symbol as IMethodSymbol;
                    if (AttributesRecognizer.IsTestAttribute(attributeSymbol)
                        || AttributesRecognizer.IsTestCaseAttribute(attributeSymbol)
                        || AttributesRecognizer.IsTestCaseSourceAttribute(attributeSymbol))
                    {
                        var diagnostic = Diagnostic.Create(Rule, methodSyntax.GetLocation(), methodSyntax.Identifier.ValueText);
                        context.ReportDiagnostic(diagnostic);
                        return;
                    }
                }
            }
        }

    }
}
