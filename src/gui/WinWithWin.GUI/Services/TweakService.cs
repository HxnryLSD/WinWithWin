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
                throw new FileNotFoundException("Tweaks configuration not found", tweaksFile);
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
            // First try native C# implementation (works without PowerShell modules)
            if (_nativeTweakService.ApplyTweak(tweakId))
            {
                return true;
            }

            // Fallback to PowerShell if native implementation doesn't support this tweak
            var tweak = FindTweak(tweakId);
            if (tweak?.Functions?.Set == null) return false;

            return await _powerShell.InvokeTweakFunctionAsync(tweak.Functions.Set);
        }

        public async Task<bool> UndoTweakAsync(string tweakId)
        {
            // First try native C# implementation (works without PowerShell modules)
            if (_nativeTweakService.UndoTweak(tweakId))
            {
                return true;
            }

            // Fallback to PowerShell if native implementation doesn't support this tweak
            var tweak = FindTweak(tweakId);
            if (tweak?.Functions?.Undo == null) return false;

            return await _powerShell.InvokeTweakFunctionAsync(tweak.Functions.Undo);
        }

        public async Task<bool> ApplyPresetAsync(string presetName)
        {
            var presetFile = Path.Combine(_configPath, "presets", $"{presetName}.json");
            
            if (!File.Exists(presetFile))
            {
                throw new FileNotFoundException("Preset not found", presetFile);
            }

            var json = await File.ReadAllTextAsync(presetFile);
            var preset = JsonConvert.DeserializeObject<PresetConfig>(json);

            if (preset?.Tweaks == null) return false;

            var success = true;
            foreach (var tweakId in preset.Tweaks)
            {
                if (!await ApplyTweakAsync(tweakId))
                {
                    success = false;
                }
            }

            return success;
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
