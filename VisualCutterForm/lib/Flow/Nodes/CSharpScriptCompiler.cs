using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Newtonsoft.Json.Linq;

namespace VisualCutterForm.Lib.Flow.Nodes
{
    public class CSharpScriptCompiler
    {
        private static readonly ConcurrentDictionary<int, CompileResult> _compileCache
            = new ConcurrentDictionary<int, CompileResult>();

        private static readonly object _nugetLock = new object();
        private static readonly Dictionary<string, string> _nugetCache = new Dictionary<string, string>();
        private static readonly LinkedList<string> _nugetLru = new LinkedList<string>();
        private const int MaxNugetCacheEntries = 64;

        public class CompileResult
        {
            public Type CompiledType { get; set; }
            public object CompiledInstance { get; set; }
            public string Error { get; set; }
            public List<string> Diagnostics { get; set; } = new List<string>();
        }

        public CompileResult Compile(string sourceCode, string extraReferences, string nugetPackages, bool debug)
        {
            var hash = (sourceCode ?? "").GetHashCode();
            if (_compileCache.TryGetValue(hash, out var cached))
                return cached;

            var result = new CompileResult();
            try
            {
                var code = sourceCode ?? "";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);

                var references = new List<MetadataReference>
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Drawing.Bitmap).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(OpenCvSharp.Mat).Assembly.Location),
                    MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location),
                };

                var mscorlib = typeof(object).Assembly;
                references.Add(MetadataReference.CreateFromFile(mscorlib.Location));

                try
                {
                    var systemRuntime = Assembly.Load("System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
                    if (systemRuntime != null)
                        references.Add(MetadataReference.CreateFromFile(systemRuntime.Location));
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"System.Runtime ref error: {ex.Message}"); }

                if (!string.IsNullOrEmpty(extraReferences))
                {
                    foreach (var refPath in extraReferences.Split(new[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var path = refPath.Trim();
                        if (File.Exists(path))
                            references.Add(MetadataReference.CreateFromFile(path));
                    }
                }

                ResolveNuGetPackages(nugetPackages, references);

                var compOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithOptimizationLevel(debug ? OptimizationLevel.Debug : OptimizationLevel.Release)
                    .WithUsings("System", "System.Collections.Generic", "System.Linq",
                        "System.Drawing", "OpenCvSharp", "System.IO");

                var compilation = CSharpCompilation.Create(
                    "Computation_" + Guid.NewGuid().ToString("N"),
                    new[] { syntaxTree },
                    references,
                    compOptions);

                using (var ms = new MemoryStream())
                {
                    var emitResult = compilation.Emit(ms);

                    foreach (var d in emitResult.Diagnostics)
                    {
                        var level = d.Severity == DiagnosticSeverity.Error ? "错误" :
                                    d.Severity == DiagnosticSeverity.Warning ? "警告" : "信息";
                        result.Diagnostics.Add($"[{level}] {d}");
                    }

                    if (!emitResult.Success)
                    {
                        result.Error = string.Join("\n", emitResult.Diagnostics
                            .Where(d => d.Severity == DiagnosticSeverity.Error)
                            .Select(d => d.ToString()));
                        _compileCache.TryAdd(hash, result);
                        return result;
                    }

                    ms.Seek(0, SeekOrigin.Begin);
                    var assembly = Assembly.Load(ms.ToArray());
                    result.CompiledType = assembly.GetType("UserCode");

                    if (result.CompiledType == null)
                    {
                        result.Error = "未找到 UserCode 类。";
                        _compileCache.TryAdd(hash, result);
                        return result;
                    }

                    result.CompiledInstance = Activator.CreateInstance(result.CompiledType);
                }
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
            }

            _compileCache.TryAdd(hash, result);
            return result;
        }

        private static void ResolveNuGetPackages(string nugetPackages, List<MetadataReference> references)
        {
            if (string.IsNullOrWhiteSpace(nugetPackages))
                return;

            var packageIds = nugetPackages
                .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .Distinct();

            foreach (var pkgId in packageIds)
            {
                try
                {
                    var asmPath = DownloadAndExtractNuGet(pkgId);
                    if (asmPath != null)
                        references.Add(MetadataReference.CreateFromFile(asmPath));
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"NuGet resolve error [{pkgId}]: {ex.Message}"); }
            }
        }

        private static string DownloadAndExtractNuGet(string packageId)
        {
            lock (_nugetLock)
            {
                if (_nugetCache.TryGetValue(packageId, out var cached))
                {
                    var found = _nugetLru.Find(packageId);
                    if (found != null)
                    {
                        _nugetLru.Remove(found);
                        _nugetLru.AddFirst(found);
                    }
                    return cached;
                }
            }

            var packagesDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VisualCutter", "NuGetPackages");
            Directory.CreateDirectory(packagesDir);

            var cacheFile = Path.Combine(packagesDir, $"{packageId}.dll");
            if (File.Exists(cacheFile) && new FileInfo(cacheFile).Length > 0)
            {
                lock (_nugetLock)
                {
                    _nugetCache[packageId] = cacheFile;
                    _nugetLru.AddFirst(packageId);
                    if (_nugetLru.Count > MaxNugetCacheEntries)
                    {
                        var last = _nugetLru.Last;
                        _nugetCache.Remove(last.Value);
                        _nugetLru.RemoveLast();
                    }
                }
                return cacheFile;
            }

            try
            {
                using (var http = new HttpClient())
                {
                    http.Timeout = TimeSpan.FromSeconds(30);

                    var versionUrl = $"https://api.nuget.org/v3-flatcontainer/{packageId.ToLowerInvariant()}/index.json";
                    var versions = http.GetStringAsync(versionUrl).Result;
                    if (string.IsNullOrEmpty(versions))
                        return null;

                    var latestVersion = JObject.Parse(versions)["versions"]
                        ?.LastOrDefault()?.ToString();
                    if (string.IsNullOrEmpty(latestVersion))
                        return null;

                    var downloadUrl = $"https://api.nuget.org/v3-flatcontainer/{packageId.ToLowerInvariant()}/{latestVersion}/{packageId.ToLowerInvariant()}.{latestVersion}.nupkg";
                    var nupkgBytes = http.GetByteArrayAsync(downloadUrl).Result;
                    if (nupkgBytes == null || nupkgBytes.Length == 0)
                        return null;

                    using (var archive = new ZipArchive(
                        new MemoryStream(nupkgBytes), ZipArchiveMode.Read))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            if (entry.FullName.StartsWith("lib/") &&
                                (entry.FullName.Contains("netstandard2") ||
                                 entry.FullName.Contains("net4") ||
                                 entry.FullName.Contains("netstandard1") ||
                                 entry.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
                            {
                                var dllName = entry.Name;
                                var dllPath = Path.Combine(packagesDir, dllName);
                                if (!File.Exists(dllPath) || new FileInfo(dllPath).Length != entry.Length)
                                {
                                    using (var entryStream = entry.Open())
                                    using (var fileStream = File.Create(dllPath))
                                    {
                                        entryStream.CopyTo(fileStream);
                                    }
                                }

                                lock (_nugetLock)
                                {
                                    _nugetCache[packageId] = dllPath;
                                    _nugetLru.AddFirst(packageId);
                                    if (_nugetLru.Count > MaxNugetCacheEntries)
                                    {
                                        var last = _nugetLru.Last;
                                        _nugetCache.Remove(last.Value);
                                        _nugetLru.RemoveLast();
                                    }
                                }
                                return dllPath;
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"NuGet download error [{packageId}]: {ex.Message}"); }
            return null;
        }
    }
}
