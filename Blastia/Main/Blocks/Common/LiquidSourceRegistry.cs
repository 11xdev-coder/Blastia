using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Blastia.Main.Blocks.Common
{
    /// <summary>
    /// Global registry for tracking active liquid sources by type and position
    /// </summary>
    public static class LiquidSourceRegistry
    {
        /// <summary>
        /// Source ID -> position
        /// </summary>
        private static readonly Dictionary<int, Vector2> ActiveSources = new();
        
        /// <summary>
        /// Source ID -> liquid <c>BlockId</c>
        /// </summary>
        private static readonly Dictionary<int, ushort> SourceTypes = new();
        
        /// <summary>
        /// liquid <c>BlockId</c> -> Source IDs
        /// </summary>
        private static readonly Dictionary<ushort, List<int>> SourcesByType = new();

        /// <summary>
        /// Registers a liquid source with its unique ID, position, and liquid block type
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="position">World position of the source</param>
        /// <param name="liquidType"><c>BlockId</c> of the liquid type</param>
        public static void RegisterSource(int sourceId, Vector2 position, ushort liquidType)
        {
            ActiveSources[sourceId] = position;
            SourceTypes[sourceId] = liquidType;
            
            // register this source ID with its liquid type
            if (!SourcesByType.TryGetValue(liquidType, out var sources))
            {
                sources = [];
                SourcesByType[liquidType] = sources;
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
            if (SourceTypes.TryGetValue(sourceId, out var liquidType))
            {
                if (SourcesByType.TryGetValue(liquidType, out var sources))
                {
                    sources.Remove(sourceId);
                    
                    // remove type list if empty
                    if (sources.Count == 0)
                    {
                        SourcesByType.Remove(liquidType);
                    }
                }
                
                SourceTypes.Remove(sourceId);
            }
            
            ActiveSources.Remove(sourceId);
        }

        /// <summary>
        /// Checks if a source with ID is currently active
        /// </summary>
        /// <param name="sourceId"></param>
        /// <returns>True if the source is active, false otherwise</returns>
        public static bool IsSourceActive(int sourceId) => ActiveSources.ContainsKey(sourceId);

        /// <summary>
        /// Gets the position of a source by its ID if it exists
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="position">Output position of the source</param>
        /// <returns>True if the source exists and position was set, false otherwise</returns>
        public static bool TryGetSourcePosition(int sourceId, out Vector2 position) =>  ActiveSources.TryGetValue(sourceId, out position);

        /// <summary>
        /// Gets the liquid type of source by its ID if it exists
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="liquidType">Output liquid <c>BlockId</c></param>
        /// <returns>True if the source exists and type was set, false otherwise</returns>
        public static bool TryGetSourceType(int sourceId, out ushort liquidType) => SourceTypes.TryGetValue(sourceId, out liquidType);

        /// <summary>
        /// Gets all active sources of a specific liquid <c>BlockId</c>
        /// </summary>
        /// <param name="liquidType">Liquid <c>BlockId</c> to search for</param>
        /// <returns><c>IReadOnlyList</c> of source IDs for the specified liquid <c>BlockId</c></returns>
        public static IReadOnlyList<int> GetSourcesByType(ushort liquidType)
        {
            if (SourcesByType.TryGetValue(liquidType, out var sources))
            {
                return sources.AsReadOnly();
            }
            
            return new List<int>().AsReadOnly();
        }

        /// <summary>
        /// Gets all active sources within the specified range of a position
        /// </summary>
        /// <param name="position">Center position</param>
        /// <param name="radius"></param>
        /// <param name="liquidType">Optional liquid <c>BlockId</c> filter, <c>0</c> for any type</param>
        /// <returns>Dictionary mapping source IDs to positions within range</returns>
        public static Dictionary<int, Vector2> GetSourcesInRange(Vector2 position, float radius, ushort liquidType = 0)
        {
            var result = new Dictionary<int, Vector2>();
            float radiusSquared = radius * radius;
            
            // liquidType specified -> check only sources of that type
            if (liquidType > 0 && SourcesByType.TryGetValue(liquidType, out var sourcesOfType))
            {
                foreach (var sourceId in sourcesOfType)
                {
                    if (ActiveSources.TryGetValue(sourceId, out var sourcePos))
                    {
                        if (Vector2.DistanceSquared(position, sourcePos) <= radiusSquared)
                        {
                            result[sourceId] = sourcePos;
                        }
                    }
                }
            }
            // otherwise -> all sources
            else
            {
                foreach (var source in ActiveSources)
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
        /// Gets the nearest source of a specific liquid type to the given position
        /// </summary>
        /// <param name="position">Reference position</param>
        /// <param name="liquidType">Liquid <c>BlockId</c></param>
        /// <param name="maxDistance"></param>
        /// <param name="sourceId">Output source ID</param>
        /// <returns>True if a source was found within the max distance, false otherwise</returns>
        public static bool TryGetNearestSource(Vector2 position, ushort liquidType, float maxDistance, out int sourceId)
        {
            sourceId = -1;
            float closestDistanceSquared = maxDistance * maxDistance;
            bool found = false;
            
            if (SourcesByType.TryGetValue(liquidType, out var sources))
            {
                foreach (var id in sources)
                {
                    if (ActiveSources.TryGetValue(id, out var sourcePos))
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
        /// Gets the count of active sources, optionally filtered by liquid type
        /// </summary>
        /// <param name="liquidType">Optional liquid <c>BlockId</c> to count, <c>0</c> for all types</param>
        /// <returns>Number of active sources of the specified type</returns>
        public static int GetSourceCount(ushort liquidType = 0)
        {
            if (liquidType > 0 && SourcesByType.TryGetValue(liquidType, out var sources))
            {
                return sources.Count;
            }
            
            return liquidType > 0 ? 0 : ActiveSources.Count;
        }

        /// <summary>
        /// Clears all registered sources
        /// </summary>
        public static void ClearAllSources()
        {
            ActiveSources.Clear();
            SourceTypes.Clear();
            SourcesByType.Clear();
        }
    }
}
