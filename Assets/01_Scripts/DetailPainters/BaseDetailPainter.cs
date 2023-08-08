using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGeneration.DetailPainters
{
    [Serializable]
    public class TerrainDetailConfig : IEquatable<TerrainDetailConfig>
    {
        [Header("Grass Billboard Configuration")]
        public Texture2D billboardTexture;
        public Color healthyColor = new Color(67f / 255f, 83f / 85f, 14f / 85f, 1f);
        public Color dryColor = new Color(41f / 51f, 188f / 255f, 26f / 255f, 1f);

        [Header("Detail Mesh Configuration")]
        public GameObject detailPrefab;

        [Header("General Configuration")]
        public float minWidth = 1f;
        public float maxWidth = 2f;
        public float minHeight = 1f;
        public float maxHeight = 2f;

        public int noiseSeed = 0;
        public float noiseSpread = 0.1f;
        [Range(0f, 1f)] public float holeEdgePadding = 0f;

        public override bool Equals(object obj)
        {
            return Equals(obj as TerrainDetailConfig);
        }

        public bool Equals(TerrainDetailConfig other)
        {
            return other != null &&
                   EqualityComparer<Texture2D>.Default.Equals(billboardTexture, other.billboardTexture) &&
                   healthyColor.Equals(other.healthyColor) &&
                   dryColor.Equals(other.dryColor) &&
                   EqualityComparer<GameObject>.Default.Equals(detailPrefab, other.detailPrefab) &&
                   minWidth == other.minWidth &&
                   maxWidth == other.maxWidth &&
                   minHeight == other.minHeight &&
                   maxHeight == other.maxHeight &&
                   noiseSeed == other.noiseSeed &&
                   noiseSpread == other.noiseSpread &&
                   holeEdgePadding == other.holeEdgePadding;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(billboardTexture);
            hash.Add(healthyColor);
            hash.Add(dryColor);
            hash.Add(detailPrefab);
            hash.Add(minWidth);
            hash.Add(maxWidth);
            hash.Add(minHeight);
            hash.Add(maxHeight);
            hash.Add(noiseSeed);
            hash.Add(noiseSpread);
            hash.Add(holeEdgePadding);
            return hash.ToHashCode();
        }
    }

    public class BaseDetailPainter : MonoBehaviour
    {
        [SerializeField]
        [Range(0f, 1f)] protected float strength = 1f;

        public virtual void Execute(ProcGenManager.GenerationData generationData, int biomeIndex = -1, BiomeConfigSO biome = null)
        {
            Debug.LogError($"No implementation of Execute function for {gameObject.name}.");
        }

        public virtual List<TerrainDetailConfig> RetrieveTerrainDetails()
        {
            return null;
        }
    }
}