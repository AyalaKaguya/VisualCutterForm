using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace VisualMaster.WorkFlow.Nodes
{
    [NodeCategory("运算", "代码运算")]
    public class ComputationNode : FlowNode
    {
        [NodeProperty("源代码", Category = "运算")]
        public string SourceCode { get; set; } =
@"public class UserCode
{
    // ---- 输入: 系统根据同名 Pin 自动填充 ----
    public OpenCvSharp.Mat Source;

    // ---- 输出: 系统执行后根据同名 Pin 自动读取 ----
    public OpenCvSharp.Mat Result;

    // 注入 FlowContext 对象 (自动赋值)
    public VisualMaster.WorkFlow.FlowContext Context;

    public void Execute()
    {
        // Context.Log(""message"")      —— 使用 Context 输出日志
        // Context.LogWarning(""warn"")  —— 输出警告
        // Context.LogError(""error"")   —— 输出错误

        // Result = Source.Clone();
    }
}";

        [NodeProperty("额外引用", Category = "运算")]
        public string ExtraReferences { get; set; } = "";

        [NodeProperty("NuGet包", Category = "运算")]
        public string NuGetPackages { get; set; } = "";

        private static readonly CSharpScriptCompiler _compiler = new CSharpScriptCompiler();

        private CSharpScriptCompiler.CompileResult _compileResult;
        private object _compiledInstance;
        private Type _compiledType;
        private Action _executeDelegate;
        private string _lastError;
        private List<string> _compileErrors = new List<string>();

        public bool IsDebug { get; set; } = true;

        public string LastError => _lastError;
        public IReadOnlyList<string> CompileErrors => _compileErrors.AsReadOnly();

        public bool Compile()
        {
            _lastError = null;
            _compileErrors = new List<string>();
            _compiledInstance = null;
            _compiledType = null;
            _compileResult = null;

            var result = _compiler.Compile(SourceCode, ExtraReferences, NuGetPackages, IsDebug);
            _compileErrors = result.Diagnostics ?? new List<string>();
            _lastError = result.Error;

            if (result.Error != null)
                return false;

            _compileResult = result;
            _compiledType = result.CompiledType;
            _compiledInstance = Activator.CreateInstance(result.CompiledType);
            _executeDelegate = null;
            if (result.ExecuteMethod != null)
                _executeDelegate = (Action)Delegate.CreateDelegate(
                    typeof(Action), _compiledInstance, result.ExecuteMethod);
            return true;
        }

        public List<string> GetUserFields()
        {
            if (_compiledType == null) return new List<string>();

            return _compiledType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Select(f => f.Name)
                .ToList();
        }

        public override async Task ExecuteAsync(FlowContext context, CancellationToken cancellationToken)
        {
            if (_compiledType == null || _compiledInstance == null)
            {
                var ok = Compile();
                if (!ok)
                    throw new InvalidOperationException($"编译失败:\n{_lastError}");
            }

            cancellationToken.ThrowIfCancellationRequested();

            _compileResult.ContextField?.SetValue(_compiledInstance, context);

            BindInputsToCompiled(context);
            BindInputsToProperties(context);

            if (_executeDelegate != null)
                _executeDelegate();
            else if (_compileResult.ExecuteMethod == null)
                throw new InvalidOperationException("UserCode 类缺少 Execute() 方法。");
            else
                _compileResult.ExecuteMethod.Invoke(_compiledInstance, null);

            ReadOutputsFromCompiled(context);
            WriteOutputsFromProperties(context);
        }

        private void BindInputsToCompiled(FlowContext context)
        {
            if (_compileResult?.InputFields == null || _compiledInstance == null) return;

            foreach (var pin in Inputs)
            {
                if (!pin.IsConnected) continue;

                if (!_compileResult.InputFields.TryGetValue(pin.Name, out var field))
                    continue;

                var val = pin.GetValue(context);
                if (val != null && field.FieldType.IsAssignableFrom(val.GetType()))
                    field.SetValue(_compiledInstance, val);
            }
        }

        private void ReadOutputsFromCompiled(FlowContext context)
        {
            if (_compileResult?.OutputFields == null || _compiledInstance == null) return;

            foreach (var pin in Outputs)
            {
                if (!_compileResult.OutputFields.TryGetValue(pin.Name, out var field))
                    continue;

                var val = field.GetValue(_compiledInstance);
                if (val != null)
                    pin.SetValue(context, val);
            }
        }
    }
}
