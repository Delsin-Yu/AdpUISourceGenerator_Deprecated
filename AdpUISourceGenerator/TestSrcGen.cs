using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace AdpUISourceGenerator
{
    [Generator]
    public class TestSrcGen : ISourceGenerator
    {
        private readonly string[] m_AccessModifiers = new string[] { "private", "public", "internal", "protected" };

        public void Execute(GeneratorExecutionContext context)
        {
            var nameSpaceNodes = context.Compilation.SyntaxTrees.SelectMany(x => x.GetRoot().DescendantNodesAndSelf().OfType<NamespaceDeclarationSyntax>());
            foreach (var namespaceNode in nameSpaceNodes)
            {
                var namespaceValue = ((IdentifierNameSyntax)namespaceNode.Name).Identifier.Text;
                var classDefs = namespaceNode.DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>();
                foreach (var classDef in classDefs)
                {
                    var classModifier = classDef.Modifiers.Select(x => x.Text).ToImmutableHashSet();
                    if (!classModifier.Contains("partial") || classModifier.Contains("static"))
                    {
                        continue;
                    }

                    var accessModifier = GetAccessModifier(classDef.Modifiers);
                    var className = classDef.Identifier.Text;
                    var propDeclNodes = classDef.DescendantNodes().OfType<FieldDeclarationSyntax>();

                    var contents = new StringBuilder();
                    var usings = new HashSet<string>();
                    var fields = new HashSet<(string FieldType, string FieldName)>();
                    foreach (var fieldDecalaratorSyntax in propDeclNodes)
                    {
                        var adpAttributes = GetAdpAttributeFromAttribute(fieldDecalaratorSyntax.AttributeLists).ToArray();
                        var typeKeyword = ((PredefinedTypeSyntax)fieldDecalaratorSyntax.Declaration.Type).Keyword.Value;
                        var fieldAccessModifier = GetAccessModifier(fieldDecalaratorSyntax.Modifiers);
                        foreach (var variableDeclaratorSyntax in fieldDecalaratorSyntax.Declaration.Variables)
                        {
                            var variableName = variableDeclaratorSyntax.Identifier.Text;

                            string variableNameCleaned;
                            if (variableName.StartsWith("m_"))
                            {
                                variableNameCleaned = variableName.Substring(2);
                            }
                            else if (variableName.StartsWith("_"))
                            {
                                variableNameCleaned = variableName.Substring(1);
                            }
                            else
                            {
                                variableNameCleaned = variableName;
                            }
                            var basePropertyNames = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(variableNameCleaned);

                            foreach (var (attributeAccessModifier, attributeType, attributeSyntax) in adpAttributes)
                            {
                                switch (attributeType)
                                {
                                    case AdpAttribute.Null:
                                        break;
                                    case AdpAttribute.TMP:
                                        var fieldName = $"{basePropertyNames}_TMP";
                                        usings.Add("TMPro");
                                        fields.Add(("TextMeshProUGUI", fieldName));
                                        contents.AppendLine($$"""
                                                    {{attributeAccessModifier}} {{typeKeyword}} {{basePropertyNames}}
                                                    {
                                                        set
                                                        {
                                                            {{fieldName}}.text = value;
                                                        }
                                                    }
                                            """);
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                        var propertyAccessModifier = GetAccessModifier(fieldDecalaratorSyntax.Modifiers);
                    }

                    context.AddSource($"{className}.genrerated.cs", $$"""
                        {{string.Join("\n", usings.OrderBy(x => x).Select(x => $"using {x};"))}}

                        namespace {{namespaceValue}}
                        {
                            {{accessModifier}} partial class {{className}}
                            {
                        {{string.Join("\n", fields.OrderBy(x => x.FieldType).ThenBy(x => x.FieldName).Select(x => $"        [SerializeField] private {x.FieldType} {x.FieldName};"))}}

                        {{contents}}
                            }
                        }
                        """);
                }
            }
        }


        private enum AdpAttribute
        {
            Null,
            TMP
        }

        private IEnumerable<(string AccessModifier, AdpAttribute AttributeType, AttributeSyntax Attribute)> GetAdpAttributeFromAttribute(in SyntaxList<AttributeListSyntax> syntaxes) => syntaxes.SelectMany(x => x.Attributes.Select(ParseAttribute));

		private enum AccessModifier
		{
			Private,
			Protected,
			Public
		}

        private (string AccessModifier, AdpAttribute AdpAttributeType, AttributeSyntax Syntax) ParseAttribute(AttributeSyntax syntax)
        {
            AdpAttribute adpAttribute;
            switch (((SimpleNameSyntax)syntax.Name).Identifier.Text)
            {
                case "TMPText":
                    adpAttribute = AdpAttribute.TMP;
                    break;
                default:
                    adpAttribute = AdpAttribute.Null;
                    break;
            }

            string accessModifier = null;
            if(adpAttribute != AdpAttribute.Null)
            {
                var accessModifierSyntax = syntax.ArgumentList.Arguments.First(x => x.ToString().Contains("AccessModifier"));
                switch (((MemberAccessExpressionSyntax)accessModifierSyntax.Expression).Name.Identifier.Text)
                {
                    case "Private":
                        accessModifier = "private";
                        break;
                    case "Protected":
                        accessModifier = "protected";
                        break;
                    case "Public":
                        accessModifier = "public";
                        break;
                }
            }

            return (accessModifier, adpAttribute, syntax);
        }

        private string GetAccessModifier(SyntaxTokenList syntaxTokens) => syntaxTokens.Select(x => x.Text).Intersect(m_AccessModifiers).FirstOrDefault() ?? "private";


        public void Initialize(GeneratorInitializationContext context)
        {
            if (!Debugger.IsAttached)
            {
                //Debugger.Launch();
            }
        }
    }
}
