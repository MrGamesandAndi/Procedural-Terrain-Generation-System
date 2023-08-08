using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGeneration
{
    [System.Serializable]
    public class BiomeConfig
    {
        public BiomeConfigSO biome;
        [Range(0f, 1f)] public float weighting = 1f;
    }

    [CreateAssetMenu(fileName = "Procedural Generation Config", menuName = "Procedural Generation/Procedural Generation Configuration", order = -1)]
    public class ProcGenConfigSO : ScriptableObject
    {
        public List<BiomeConfig> biomes;
        public GameObject biomeGenerators;
        public GameObject initialHeightModifier;
        public GameObject heightPostProcessingModifier;
        public GameObject paintingPostProcessingModifier;
        public GameObject detailPaintingPostProcessingModifier;
        public float waterHeight = 15f;

        public int NumBiomes => biomes.Count;
        public float TotalWeighting
        {
            get
            {
                float sum = 0f;

                foreach (var config in biomes)
                {
                    sum += config.weighting;
                }

                return sum;
            }
        }
    }
}
