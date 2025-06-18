using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Blastia.Main.Blocks.Common
{
    /// <summary>
    /// Global registry for tracking active liquid sources by type and position.
    /// </summary>
    public static class LiquidSourceRegistry
    {
        // Dictionary mapping source ID to position
        private static readonly Dictionary<int, Vector2> _activeSources = new();
        
        // Dictionary mapping source ID to liquid type
        private static readonly Dictionary<int, ushort> _sourceTypes = new();
        
        // Dictionary mapping liquid type to list of source IDs
        private static readonly Dictionary<ushort, List<int>> _sourcesByType = new();

        /// <summary>
        /// Registers a liquid source with its unique ID, position, and liquid block type
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="position">World position of the source</param>
        /// <param name="liquidType"><c>BlockId</c> of the liquid type</param>
        public static void RegisterSource(int sourceId, Vector2 position, ushort liquidType)
        {
            _activeSources[sourceId] = position;
            _sourceTypes[sourceId] = liquidType;
            
            // register this source ID with its liquid type
            if (!_sourcesByType.TryGetValue(liquidType, out var sources))
            {
                sources = [];
                _sourcesByType[liquidType] = sources;
            }
            
            if (!sources.Contains(sourceId))
            {
                sources.Add(sourceId);
            }
        }

        /// <summary>
        /// Unregisters a liquid source by ID
        /// </summary>
        /// <param name="sourceId"></param>
        public static void UnregisterSource(int sourceId)
        {
            if (_sourceTypes.TryGetValue(sourceId, out var liquidType))
            {
                if (_sourcesByType.TryGetValue(liquidType, out var sources))
                {
                    sources.Remove(sourceId);
                    
                    // remove type list if empty
                    if (sources.Count == 0)
                    {
                        _sourcesByType.Remove(liquidType);
                    }
                }
                
                _sourceTypes.Remove(sourceId);
            }
            
            _activeSources.Remove(sourceId);
        }

        /// <summary>
        /// Checks if a source with ID is currently active
        /// </summary>
        /// <param name="sourceId"></param>
        /// <returns>True if the source is active, false otherwise</returns>
        public static bool IsSourceActive(int sourceId) => _activeSources.ContainsKey(sourceId);

        /// <summary>
        /// Gets the position of a source by its ID, if it exists.
        /// </summary>
        /// <param name="sourceId">Source ID to look up</param>
        /// <param name="position">Output position of the source</param>
        /// <returns>True if the source exists and position was set, false otherwise</returns>
        public static bool TryGetSourcePosition(int sourceId, out Vector2 position) =>  _activeSources.TryGetValue(sourceId, out position);

        /// <summary>
        /// Gets the liquid type of a source by its ID, if it exists.
        /// </summary>
        /// <param name="sourceId">Source ID to look up</param>
        /// <param name="liquidType">Output liquid block ID</param>
        /// <returns>True if the source exists and type was set, false otherwise</returns>
        public static bool TryGetSourceType(int sourceId, out ushort liquidType) => _sourceTypes.TryGetValue(sourceId, out liquidType);

        /// <summary>
        /// Gets all active sources of a specific liquid type.
        /// </summary>
        /// <param name="liquidType">Liquid block ID to search for</param>
        /// <returns>IReadOnlyList of source IDs for the specified liquid type</returns>
        public static IReadOnlyList<int> GetSourcesByType(ushort liquidType)
        {
            if (_sourcesByType.TryGetValue(liquidType, out var sources))
            {
                return sources.AsReadOnly();
            }
            
            return new List<int>().AsReadOnly();
        }

        /// <summary>
        /// Gets all active sources within the specified range of a position.
        /// </summary>
        /// <param name="position">Center position to check from</param>
        /// <param name="radius">Radius to check within</param>
        /// <param name="liquidType">Optional liquid type filter, 0 for any type</param>
        /// <returns>Dictionary mapping source IDs to positions within range</returns>
        public static Dictionary<int, Vector2> GetSourcesInRange(Vector2 position, float radius, ushort liquidType = 0)
        {
            var result = new Dictionary<int, Vector2>();
            float radiusSquared = radius * radius;
            
            // If liquidType is specified, only check sources of that type
            if (liquidType > 0 && _sourcesByType.TryGetValue(liquidType, out var sourcesOfType))
            {
                foreach (var sourceId in sourcesOfType)
                {
                    if (_activeSources.TryGetValue(sourceId, out var sourcePos))
                    {
                        if (Vector2.DistanceSquared(position, sourcePos) <= radiusSquared)
                        {
                            result[sourceId] = sourcePos;
                        }
                    }
                }
            }
            // Otherwise check all sources
            else
            {
                foreach (var source in _activeSources)
                {
                    if (Vector2.DistanceSquared(position, source.Value) <= radiusSquared)
                    {
                        result[source.Key] = source.Value;
                    }
                }
            }
            
            return result;
        }

        /// <summary>
        /// Gets the nearest source of a specific liquid type to the given position.
        /// </summary>
        /// <param name="position">Reference position</param>
        /// <param name="liquidType">Liquid type to search for</param>
        /// <param name="maxDistance">Maximum distance to search</param>
        /// <param name="sourceId">Output source ID of the nearest source</param>
        /// <returns>True if a source was found within the max distance, false otherwise</returns>
        public static bool TryGetNearestSource(Vector2 position, ushort liquidType, float maxDistance, out int sourceId)
        {
            sourceId = -1;
            float closestDistanceSquared = maxDistance * maxDistance;
            bool found = false;
            
            if (_sourcesByType.TryGetValue(liquidType, out var sources))
            {
                foreach (var id in sources)
                {
                    if (_activeSources.TryGetValue(id, out var sourcePos))
                    {
                        float distanceSquared = Vector2.DistanceSquared(position, sourcePos);
                        if (distanceSquared < closestDistanceSquared)
                        {
                            closestDistanceSquared = distanceSquared;
                            sourceId = id;
                            found = true;
                        }
                    }
                }
            }
            
            return found;
        }

        /// <summary>
        /// Gets the count of active sources, optionally filtered by liquid type.
        /// </summary>
        /// <param name="liquidType">Optional liquid type to count, 0 for all types</param>
        /// <returns>Number of active sources of the specified type</returns>
        public static int GetSourceCount(ushort liquidType = 0)
        {
            if (liquidType > 0 && _sourcesByType.TryGetValue(liquidType, out var sources))
            {
                return sources.Count;
            }
            
            return liquidType > 0 ? 0 : _activeSources.Count;
        }

        /// <summary>
        /// Clears all registered sources
        /// </summary>
        public static void ClearAllSources()
        {
            _activeSources.Clear();
            _sourceTypes.Clear();
            _sourcesByType.Clear();
        }
    }
}
