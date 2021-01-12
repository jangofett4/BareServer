using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

using BareServer.Core;
using System.Reflection;

namespace BareServer.Backends
{
    public class BHtmlCompiler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static string PageTemplate { get; } =
@"
using System;
using System.Text;
using System.Collections.Generic;

using BareServer.Core;
using BareServer.Backends;

namespace %AppName%
{
    public class CompiledPage%Count% : Page
    {
        public CompiledPage%Count%(App app) : base(app, ""%Route%"")
        {
            Response = (appParam, ctxParam) => {
                Request = ctxParam;
                return RouteResponse.FromString(Run());
            };
        }

        public override string Run()
        {
            base.Run();
            %Code%
            return ToString();
        }
    }
}";
        private static int Count { get; set; } = 0;

        public static Regex TagRegex { get; } = new Regex(@"(?<=<%)((.|\n)*?)(?=%>)", RegexOptions.Compiled);

        public static Page Compile(App app, string source, string route)
        {
            var template = PageTemplate;
            template = template.Replace("%AppName%", app.Name);
            template = template.Replace("%Count%", (Count++).ToString());
            template = template.Replace("%Route%", route);

            var parts = Parse(source);
            var code = new StringBuilder();
            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part.Content))
                    continue; // skip empty
                // Write raw
                if (part.Type == BHtmlPart.PartType.Html)
                    code.AppendLine($"Write(@\" { part.Content.Replace("\"", "\"\"") } \");");
                // Write code
                else
                    code.AppendLine(part.Content);
            }

            template = template.Replace("%Code%", code.ToString());

            var sourcestr = SourceText.From(template);
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_3);
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(sourcestr, options);

            var dotNetCoreDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
            var refs = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(Path.Combine(dotNetCoreDir, "System.Runtime.dll")), // TODO: dirty hack to include runtime
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(App).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Page).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).Assembly.Location),
            };

            var res = CSharpCompilation.Create(null, new [] { syntaxTree }, refs, new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default
            ));
            using (var ms = new MemoryStream())
            {
                var result = res.Emit(ms);
                if (!result.Success)
                {
                    Logger.Error("Page could not be compiled, {} errors:", result.Diagnostics.Length);
                    foreach (var d in result.Diagnostics)
                        Logger.Error(d.ToString());
                    return null;
                }
                else
                {
                    ms.Position = 0; // rewind
                    var loader = new MemoryAssemblyLoaderContext();
                    var asm = loader.LoadFromStream(ms);
                    var pageclass = asm.GetType($"{ app.Name }.CompiledPage{ Count - 1}");
                    return (Page)pageclass.GetConstructor(new[]{ typeof(App) }).Invoke(new[] { app });
                    // .GetMethod("Run", 0, new Type[0]);
                }
            }
        }

        public class MemoryAssemblyLoaderContext : AssemblyLoadContext
        {
            public MemoryAssemblyLoaderContext() : base(true)
            {
            }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                return null;
            }
        }

        // Need better parser. TODO:
        public static BHtmlPart[] Parse(string source)
        {
            var result = new List<BHtmlPart>();
            var code = false;
            for(;;)
            {
                int index = -1;

                if (code == false)
                    index = source.IndexOf("<%");
                else
                    index = source.IndexOf("%>");

                if (index < 0)
                {
                    result.Add(new BHtmlPart(source, code ? BHtmlPart.PartType.Code : BHtmlPart.PartType.Html));
                    break;
                }


                result.Add(new BHtmlPart(source.Substring(0, index), code ? BHtmlPart.PartType.Code : BHtmlPart.PartType.Html));
                code = !code;

                source = source.Substring(index + 2);
            }

            return result.ToArray();
        }

        public class BHtmlPart
        {
            public enum PartType
            {
                Html,
                Code,
                AutoWrite
            }

            public string Content { get; }
            public PartType Type { get; }

            public BHtmlPart(string content, PartType type)
            {
                Content = content;
                Type = type;
            }
        }
    }
}