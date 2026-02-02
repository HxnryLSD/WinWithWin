using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace WinWithWin.GUI.Services
{
    /// <summary>
    /// Service for managing favorite tweaks
    /// </summary>
    public class FavoritesService
    {
        private readonly string _favoritesFilePath;
        private HashSet<string> _favorites;

        public event EventHandler<string>? FavoriteAdded;
        public event EventHandler<string>? FavoriteRemoved;

        public FavoritesService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WinWithWin"
            );
            
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            _favoritesFilePath = Path.Combine(appDataPath, "favorites.json");
            _favorites = LoadFavorites();
        }

        /// <summary>
        /// Gets all favorite tweak IDs
        /// </summary>
        public IReadOnlyCollection<string> GetFavorites()
        {
            return _favorites.ToList().AsReadOnly();
        }

        /// <summary>
        /// Checks if a tweak is marked as favorite
        /// </summary>
        public bool IsFavorite(string tweakId)
        {
            return _favorites.Contains(tweakId);
        }

        /// <summary>
        /// Adds a tweak to favorites
        /// </summary>
        public void AddFavorite(string tweakId)
        {
            if (_favorites.Add(tweakId))
            {
                SaveFavorites();
                FavoriteAdded?.Invoke(this, tweakId);
            }
        }

        /// <summary>
        /// Removes a tweak from favorites
        /// </summary>
        public void RemoveFavorite(string tweakId)
        {
            if (_favorites.Remove(tweakId))
            {
                SaveFavorites();
                FavoriteRemoved?.Invoke(this, tweakId);
            }
        }

        /// <summary>
        /// Toggles favorite status for a tweak
        /// </summary>
        public bool ToggleFavorite(string tweakId)
        {
            if (IsFavorite(tweakId))
            {
                RemoveFavorite(tweakId);
                return false;
            }
            else
            {
                AddFavorite(tweakId);
                return true;
            }
        }

        private HashSet<string> LoadFavorites()
        {
            try
            {
                if (File.Exists(_favoritesFilePath))
                {
                    var json = File.ReadAllText(_favoritesFilePath);
                    var list = JsonConvert.DeserializeObject<List<string>>(json);
                    return list != null ? new HashSet<string>(list) : new HashSet<string>();
                }
            }
            catch
            {
                // Ignore errors, return empty set
            }

            return new HashSet<string>();
        }

        private void SaveFavorites()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_favorites.ToList(), Formatting.Indented);
                File.WriteAllText(_favoritesFilePath, json);
            }
            catch
            {
                // Ignore save errors
            }
        }
    }
}
