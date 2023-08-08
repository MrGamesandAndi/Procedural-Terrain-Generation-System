using UnityEngine;

namespace ProceduralGeneration.BiomeGenerators
{
    public class BaseBiomeMapGenerator : MonoBehaviour
    {
        public virtual void Execute(ProcGenManager.GenerationData generationData)
        {
            Debug.LogError($"No implementation of Execute function for {gameObject.name}.");
        }
    }
}