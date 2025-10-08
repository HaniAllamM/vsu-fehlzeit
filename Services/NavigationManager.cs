using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace FehlzeitApp.Services
{
    /// <summary>
    /// Manages page navigation with caching to maintain state between page switches
    /// </summary>
    public class NavigationManager
    {
        private static NavigationManager? _instance;
        public static NavigationManager Instance => _instance ??= new NavigationManager();

        private readonly Dictionary<Type, UserControl> _pageCache = new();
        private Frame? _navigationFrame;

        /// <summary>
        /// Initialize the navigation manager with the main frame
        /// </summary>
        /// <param name="frame">The main navigation frame</param>
        public void Initialize(Frame frame)
        {
            _navigationFrame = frame ?? throw new ArgumentNullException(nameof(frame));
        }

        /// <summary>
        /// Navigate to a page type, creating it only once and caching for future use
        /// </summary>
        /// <typeparam name="T">Type of UserControl/Page to navigate to</typeparam>
        /// <param name="constructorArgs">Arguments to pass to the page constructor</param>
        public void NavigateTo<T>(params object[] constructorArgs) where T : UserControl
        {
            if (_navigationFrame == null)
                throw new InvalidOperationException("NavigationManager not initialized. Call Initialize() first.");

            var pageType = typeof(T);

            // Check if page already exists in cache
            if (!_pageCache.ContainsKey(pageType))
            {
                try
                {
                    // Create new instance only if not cached
                    var page = (UserControl)Activator.CreateInstance(pageType, constructorArgs)!;
                    _pageCache[pageType] = page;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to create instance of {pageType.Name}", ex);
                }
            }

            // Navigate to cached page - this preserves all state
            _navigationFrame.Content = _pageCache[pageType];
        }

        /// <summary>
        /// Navigate to a page by type name
        /// </summary>
        /// <param name="pageTypeName">Name of the page type</param>
        /// <param name="constructorArgs">Arguments to pass to the page constructor</param>
        public void NavigateTo(string pageTypeName, params object[] constructorArgs)
        {
            var pageType = Type.GetType($"FehlzeitApp.Views.{pageTypeName}");
            if (pageType == null)
                throw new ArgumentException($"Page type '{pageTypeName}' not found");

            NavigateToType(pageType, constructorArgs);
        }

        /// <summary>
        /// Navigate to a specific type
        /// </summary>
        /// <param name="pageType">Type to navigate to</param>
        /// <param name="constructorArgs">Constructor arguments</param>
        private void NavigateToType(Type pageType, params object[] constructorArgs)
        {
            if (_navigationFrame == null)
                throw new InvalidOperationException("NavigationManager not initialized. Call Initialize() first.");

            if (!_pageCache.ContainsKey(pageType))
            {
                try
                {
                    var page = (UserControl)Activator.CreateInstance(pageType, constructorArgs)!;
                    _pageCache[pageType] = page;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to create instance of {pageType.Name}", ex);
                }
            }

            _navigationFrame.Content = _pageCache[pageType];
        }

        /// <summary>
        /// Check if a page is already cached
        /// </summary>
        /// <typeparam name="T">Type of page to check</typeparam>
        /// <returns>True if page is cached</returns>
        public bool IsPageCached<T>() where T : UserControl
        {
            return _pageCache.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Get the current page if it exists in cache
        /// </summary>
        /// <typeparam name="T">Type of page to get</typeparam>
        /// <returns>Cached page instance or null</returns>
        public T? GetCachedPage<T>() where T : UserControl
        {
            var pageType = typeof(T);
            return _pageCache.ContainsKey(pageType) ? (T)_pageCache[pageType] : null;
        }

        /// <summary>
        /// Clear all cached pages (useful for logout or memory cleanup)
        /// </summary>
        public void ClearCache()
        {
            _pageCache.Clear();
        }

        /// <summary>
        /// Remove a specific page from cache
        /// </summary>
        /// <typeparam name="T">Type of page to remove</typeparam>
        public void RemoveFromCache<T>() where T : UserControl
        {
            var pageType = typeof(T);
            if (_pageCache.ContainsKey(pageType))
            {
                _pageCache.Remove(pageType);
            }
        }

        /// <summary>
        /// Get the count of cached pages
        /// </summary>
        public int CachedPageCount => _pageCache.Count;

        /// <summary>
        /// Get all cached page types
        /// </summary>
        public IEnumerable<Type> CachedPageTypes => _pageCache.Keys;
    }
}
