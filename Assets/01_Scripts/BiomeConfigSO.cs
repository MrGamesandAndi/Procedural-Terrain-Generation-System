using ProceduralGeneration.DetailPainters;
using ProceduralGeneration.TexturePainters;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGeneration
{
    [CreateAssetMenu(fileName = "Biome Config", menuName = "Procedural Generation/Biome Configuration", order = -1)]
    public class BiomeConfigSO : ScriptableObject
    {
        public string biomeName;
        public GameObject heightModifier;
        public GameObject terrainPainter;
        public GameObject objectPlacer;
        public GameObject detailPainter;

        [Range(0f, 1f)] public float minIntensity = 0.5f;
        [Range(0f, 1f)] public float maxIntensity = 1f;
        [Range(0f, 1f)] public float minDecayRate = 0.01f;
        [Range(0f, 1f)] public float maxDecayRate = 0.02f;

        public List<TextureConfig> RetrieveTextures()
        {
            if (terrainPainter == null)
            {
                return null;
            }

            //Extract textures from painters
            List<TextureConfig> allTextures = new List<TextureConfig>();
            BaseTexturePainter[] allPainters = terrainPainter.GetComponents<BaseTexturePainter>();

            foreach (var painter in allPainters)
            {
                var painterTextures = painter.RetrieveTextures();

                if (painterTextures == null || painterTextures.Count == 0)
                {
                    continue;
                }

                allTextures.AddRange(painterTextures);
            }

            return allTextures;
        }

        public List<TerrainDetailConfig> RetrieveTerrainDetails()
        {
            if (detailPainter == null)
            {
                return null;
            }

            //Extract all terrain details from every painter
            List<TerrainDetailConfig> allTerrainDetails = new List<TerrainDetailConfig>();
            BaseDetailPainter[] allPainters = detailPainter.GetComponents<BaseDetailPainter>();

            foreach (var painter in allPainters)
            {
                var terrainDetails = painter.RetrieveTerrainDetails();

                if (terrainDetails == null || terrainDetails.Count == 0)
                {
                    continue;
                }

                allTerrainDetails.AddRange(terrainDetails);
            }

            return allTerrainDetails;
        }
    }
}