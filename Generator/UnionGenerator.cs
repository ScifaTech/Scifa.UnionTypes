using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace UnionTypes.Generator
{
    [Generator]
    public class UnionGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new UnionTypeReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var receiver = context.SyntaxReceiver as UnionTypeReceiver ?? throw new InvalidCastException("Generator did not initialize properly");

            if (UnionGeneratorImpl.TryGetInstance(context.Compilation, out var gen))
            {
                foreach (var candidate in receiver.Candidates)
                {
                    if (gen.TryGetSource(candidate, out var hintName, out var source))
                    {
                        context.AddSource(hintName, source);
                    }
                }
            }
        }
    }
}