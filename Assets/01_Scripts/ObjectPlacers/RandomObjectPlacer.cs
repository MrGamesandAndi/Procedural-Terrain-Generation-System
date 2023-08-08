using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGeneration.ObjectPlacers
{
    public class RandomObjectPlacer : BaseObjectPlacer
    {
        public override void Execute(ProcGenManager.GenerationData generationData, int biomeIndex = -1, BiomeConfigSO biome = null)
        {
            base.Execute(generationData, biomeIndex, biome);

            //Get potential spawn locations
            List<Vector3> candidateLocations = GetAllLocationsForBiome(generationData, biomeIndex);

            ExecuteSimpleSpawning(generationData, candidateLocations);
        }
    }
}