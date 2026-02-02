using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;

namespace WinWithWin.GUI.Services
{
    public class PowerShellService : IDisposable
    {
        private Runspace? _runspace;
        private readonly string _modulePath;
        private bool _isInitialized;
        private bool _isAvailable;

        public bool IsAvailable => _isAvailable;

        public PowerShellService()
        {
            var basePath = PathHelper.ApplicationDirectory;
            _modulePath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", "src", "core"));

            // Fallback for published app
            if (!Directory.Exists(_modulePath))
            {
                _modulePath = Path.Combine(basePath, "PowerShell");
            }

            try
            {
                _runspace = RunspaceFactory.CreateRunspace();
                _runspace.Open();
                _isAvailable = true;
            }
            catch (Exception)
            {
                // PowerShell not available - will run in limited mode
                _isAvailable = false;
                _runspace = null;
            }
        }

        private async Task InitializeAsync()
        {
            if (_isInitialized || !_isAvailable || _runspace == null) return;

            using var ps = PowerShell.Create();
            ps.Runspace = _runspace;

            // Import modules
            var coreModule = Path.Combine(_modulePath, "WinWithWin.psm1");
            if (File.Exists(coreModule))
            {
                ps.AddCommand("Import-Module")
                  .AddParameter("Name", coreModule)
                  .AddParameter("Force")
                  .AddParameter("DisableNameChecking");

                await Task.Run(() => ps.Invoke());
                ps.Commands.Clear();
            }

            // Import function modules
            var functionsPath = Path.Combine(Path.GetDirectoryName(_modulePath) ?? "", "functions");
            if (Directory.Exists(functionsPath))
            {
                foreach (var module in Directory.GetFiles(functionsPath, "*.psm1"))
                {
                    ps.AddCommand("Import-Module")
                      .AddParameter("Name", module)
                      .AddParameter("Force")
                      .AddParameter("DisableNameChecking");

                    await Task.Run(() => ps.Invoke());
                    ps.Commands.Clear();
                }
            }

            _isInitialized = true;
        }

        public async Task<bool> TestTweakAsync(string functionName)
        {
            if (!_isAvailable || _runspace == null) return false;
            
            await InitializeAsync();

            using var ps = PowerShell.Create();
            ps.Runspace = _runspace;

            ps.AddCommand(functionName);

            var results = await Task.Run(() => ps.Invoke());

            if (ps.HadErrors || results.Count == 0)
            {
                return false;
            }

            if (results[0].BaseObject is bool boolResult)
            {
                return boolResult;
            }

            return false;
        }

        public async Task<bool> InvokeTweakFunctionAsync(string functionName)
        {
            if (!_isAvailable || _runspace == null) return false;
            
            await InitializeAsync();

            using var ps = PowerShell.Create();
            ps.Runspace = _runspace;

            ps.AddCommand(functionName);

            try
            {
                var results = await Task.Run(() => ps.Invoke());

                if (ps.HadErrors)
                {
                    foreach (var error in ps.Streams.Error)
                    {
                        Console.WriteLine($"PowerShell Error: {error}");
                    }
                    return false;
                }

                if (results.Count > 0 && results[0].BaseObject is bool success)
                {
                    return success;
                }

                return true; // Function completed without errors
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception invoking {functionName}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CreateRestorePointAsync(string description)
        {
            if (!_isAvailable || _runspace == null) return false;
            
            using var ps = PowerShell.Create();
            ps.Runspace = _runspace;

            ps.AddCommand("Checkpoint-Computer")
              .AddParameter("Description", description)
              .AddParameter("RestorePointType", "MODIFY_SETTINGS");

            try
            {
                await Task.Run(() => ps.Invoke());
                return !ps.HadErrors;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _runspace?.Close();
            _runspace?.Dispose();
        }
    }
}
