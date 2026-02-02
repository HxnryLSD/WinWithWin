using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WinWithWin.GUI.Models;

namespace WinWithWin.GUI.Services
{
    public class TweakService
    {
        private readonly PowerShellService _powerShell;
        private readonly NativeTweakService _nativeTweakService;
        private readonly TweakStateChecker _stateChecker;
        private readonly LocalizationService _localizationService;
        private readonly string _configPath;
        private TweakConfig? _tweakConfig;

        /// <summary>
        /// Event raised when progress updates during tweak operations.
        /// </summary>
        public event Action<int, string>? ProgressChanged;

        /// <summary>
        /// Event raised when a tweak operation starts.
        /// </summary>
        public event Action<string>? OperationStarted;

        /// <summary>
        /// Event raised when a tweak operation ends.
        /// </summary>
        public event Action? OperationEnded;

        public TweakService(PowerShellService powerShell, LocalizationService localizationService)
        {
            _powerShell = powerShell;
            _localizationService = localizationService;
            _nativeTweakService = new NativeTweakService();
            _stateChecker = new TweakStateChecker();
            _configPath = PathHelper.ConfigPath;
        }

        public async Task<List<TweakViewModel>> LoadTweaksAsync()
        {
            var tweaksFile = Path.Combine(_configPath, "tweaks.json");
            
            if (!File.Exists(tweaksFile))
            {
                throw new FileNotFoundException(
                    $"Tweaks configuration not found.\n\nExpected location: {tweaksFile}\n\nMake sure the 'config' folder is in the same directory as the executable.", 
                    tweaksFile);
            }

            var json = await File.ReadAllTextAsync(tweaksFile);
            _tweakConfig = JsonConvert.DeserializeObject<TweakConfig>(json);

            if (_tweakConfig?.Tweaks == null)
            {
                return new List<TweakViewModel>();
            }

            var viewModels = new List<TweakViewModel>();
            
            foreach (var tweak in _tweakConfig.Tweaks)
            {
                // Try to get localized name and description from locale files
                var localizedName = _localizationService.GetString($"tweaks.{tweak.Id}.name");
                var localizedDescription = _localizationService.GetString($"tweaks.{tweak.Id}.description");
                var localizedDetailedDescription = _localizationService.GetString($"tweaks.{tweak.Id}.detailedDescription");
                var localizedImpact = _localizationService.GetString($"tweaks.{tweak.Id}.impact");
                var localizedRecommendation = _localizationService.GetString($"tweaks.{tweak.Id}.recommendation");
                
                // Use localized version if available, otherwise fall back to config file
                var name = localizedName != $"tweaks.{tweak.Id}.name" ? localizedName : tweak.Name;
                var description = localizedDescription != $"tweaks.{tweak.Id}.description" ? localizedDescription : tweak.Description;
                var detailedDescription = localizedDetailedDescription != $"tweaks.{tweak.Id}.detailedDescription" 
                    ? localizedDetailedDescription 
                    : (tweak.DetailedDescription ?? "");
                var impact = localizedImpact != $"tweaks.{tweak.Id}.impact" ? localizedImpact : (tweak.Impact ?? "");
                var recommendation = localizedRecommendation != $"tweaks.{tweak.Id}.recommendation" 
                    ? localizedRecommendation 
                    : (tweak.Recommendation ?? "");
                
                var vm = new TweakViewModel
                {
                    Id = tweak.Id,
                    Name = name,
                    Description = description,
                    DetailedDescription = detailedDescription,
                    Impact = impact,
                    Recommendation = recommendation,
                    Category = tweak.Category,
                    Risk = tweak.Risk,
                    SupportedVersions = tweak.SupportedVersions,
                    AffectedComponents = tweak.AffectedComponents ?? System.Array.Empty<string>(),
                    IsReversible = tweak.IsReversible,
                    IsApplied = false
                };

                // Check current state using registry-based checker (fast, no PowerShell needed)
                vm.IsApplied = _stateChecker.IsTweakApplied(tweak.Id);

                viewModels.Add(vm);
            }

            return viewModels;
        }

        /// <summary>
        /// Re-checks the state of all tweaks without reloading the configuration.
        /// Call this to refresh the UI after system changes.
        /// </summary>
        public void RefreshTweakStates(IEnumerable<TweakViewModel> tweaks)
        {
            foreach (var tweak in tweaks)
            {
                tweak.IsApplied = _stateChecker.IsTweakApplied(tweak.Id);
            }
        }

        /// <summary>
        /// Checks if a specific tweak is currently applied.
        /// </summary>
        public bool IsTweakApplied(string tweakId)
        {
            return _stateChecker.IsTweakApplied(tweakId);
        }

        public async Task<bool> ApplyTweakAsync(string tweakId)
        {
            var tweakName = GetTweakName(tweakId);
            OperationStarted?.Invoke(tweakName);
            
            try
            {
                // First try native C# implementation with detailed result
                var nativeResult = _nativeTweakService.ApplyTweakWithDetails(tweakId);
                
                if (nativeResult.Success)
                {
                    return true;
                }

                // Check if it's "not supported" - then try PowerShell fallback
                if (nativeResult.ErrorMessage?.Contains("not implemented") == true)
                {
                    // Fallback to PowerShell if native implementation doesn't support this tweak
                    var tweak = FindTweak(tweakId);
                    if (tweak?.Functions?.Set == null)
                    {
                        LoggingService.LogTweakResult(tweakId, tweakName, true, false, "No implementation found (neither native nor PowerShell)");
                        return false;
                    }

                    LoggingService.LogInfo($"Falling back to PowerShell for tweak: {tweakId}");
                    var psResult = await _powerShell.InvokeTweakFunctionAsync(tweak.Functions.Set);
                    
                    if (psResult)
                    {
                        LoggingService.LogTweakResult(tweakId, tweakName, true, true, "Applied via PowerShell");
                    }
                    else
                    {
                        LoggingService.LogTweakResult(tweakId, tweakName, true, false, 
                            "PowerShell execution failed. Check if PowerShell modules are available and the function exists.");
                    }
                    return psResult;
                }

                // Native implementation failed with an actual error
                LoggingService.LogTweakResult(tweakId, tweakName, true, false, 
                    $"{nativeResult.ErrorMessage}\n{nativeResult.ErrorDetails}");
                return false;
            }
            catch (Exception ex)
            {
                LoggingService.LogTweakResult(tweakId, tweakName, true, false, ex.Message);
                LoggingService.LogError($"Exception applying tweak {tweakId}", ex);
                return false;
            }
            finally
            {
                OperationEnded?.Invoke();
            }
        }

        public async Task<bool> UndoTweakAsync(string tweakId)
        {
            var tweakName = GetTweakName(tweakId);
            OperationStarted?.Invoke(tweakName);
            
            try
            {
                // First try native C# implementation with detailed result
                var nativeResult = _nativeTweakService.UndoTweakWithDetails(tweakId);
                
                if (nativeResult.Success)
                {
                    return true;
                }

                // Check if it's "not supported" - then try PowerShell fallback
                if (nativeResult.ErrorMessage?.Contains("not implemented") == true)
                {
                    // Fallback to PowerShell if native implementation doesn't support this tweak
                    var tweak = FindTweak(tweakId);
                    if (tweak?.Functions?.Undo == null)
                    {
                        LoggingService.LogTweakResult(tweakId, tweakName, false, false, "No implementation found (neither native nor PowerShell)");
                        return false;
                    }

                    LoggingService.LogInfo($"Falling back to PowerShell for undo tweak: {tweakId}");
                    var psResult = await _powerShell.InvokeTweakFunctionAsync(tweak.Functions.Undo);
                    
                    if (psResult)
                    {
                        LoggingService.LogTweakResult(tweakId, tweakName, false, true, "Undone via PowerShell");
                    }
                    else
                    {
                        LoggingService.LogTweakResult(tweakId, tweakName, false, false, 
                            "PowerShell execution failed. Check if PowerShell modules are available and the function exists.");
                    }
                    return psResult;
                }

                // Native implementation failed with an actual error
                LoggingService.LogTweakResult(tweakId, tweakName, false, false, 
                    $"{nativeResult.ErrorMessage}\n{nativeResult.ErrorDetails}");
                return false;
            }
            catch (Exception ex)
            {
                LoggingService.LogTweakResult(tweakId, tweakName, false, false, ex.Message);
                LoggingService.LogError($"Exception undoing tweak {tweakId}", ex);
                return false;
            }
            finally
            {
                OperationEnded?.Invoke();
            }
        }

        public async Task<bool> ApplyPresetAsync(string presetName)
        {
            var presetFile = Path.Combine(_configPath, "presets", $"{presetName}.json");
            
            if (!File.Exists(presetFile))
            {
                LoggingService.LogError($"Preset file not found: {presetFile}");
                throw new FileNotFoundException("Preset not found", presetFile);
            }

            var json = await File.ReadAllTextAsync(presetFile);
            var preset = JsonConvert.DeserializeObject<PresetConfig>(json);

            if (preset?.Tweaks == null) return false;

            LoggingService.LogInfo($"Applying preset: {presetName} with {preset.Tweaks.Length} tweaks");
            
            var success = true;
            var total = preset.Tweaks.Length;
            var current = 0;
            
            foreach (var tweakId in preset.Tweaks)
            {
                current++;
                var percent = (int)((current * 100.0) / total);
                ProgressChanged?.Invoke(percent, $"Applying {tweakId}...");
                
                if (!await ApplyTweakAsync(tweakId))
                {
                    success = false;
                }
            }

            LoggingService.LogInfo($"Preset {presetName} completed. Success: {success}");
            return success;
        }

        private string GetTweakName(string tweakId)
        {
            var tweak = FindTweak(tweakId);
            return tweak?.Name ?? tweakId;
        }

        public async Task<bool> CreateRestorePointAsync(string description)
        {
            return await _powerShell.CreateRestorePointAsync(description);
        }

        private TweakDefinition? FindTweak(string tweakId)
        {
            if (_tweakConfig?.Tweaks == null) return null;

            foreach (var tweak in _tweakConfig.Tweaks)
            {
                if (tweak.Id == tweakId) return tweak;
            }

            return null;
        }
    }
}
