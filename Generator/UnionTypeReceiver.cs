using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace UnionTypes.Generator
{
    internal class UnionTypeReceiver : ISyntaxReceiver
    {
        public readonly List<TypeDeclarationSyntax> Candidates = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is TypeDeclarationSyntax typeDecl)
            {
                if (IsUnionTypeCandidate(typeDecl))
                {
                    Candidates.Add(typeDecl);
                }
            }
        }

        private bool IsUnionTypeCandidate(TypeDeclarationSyntax typeDecl)
        {
            var matchingAttrs = from list in typeDecl.AttributeLists
                                from attr in list.Attributes
                                select attr;

            return matchingAttrs.Any();
        }
    }
}