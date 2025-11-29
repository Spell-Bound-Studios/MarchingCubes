using Unity.Mathematics;
using UnityEngine;

namespace Spellbound.MarchingCubes {
    [CreateAssetMenu(menuName = "Spellbound/MarchingCubes/VoxelVolumeConfig")]
    public class VoxelVolumeConfig : ScriptableObject {

        [Range(1, 255)] public byte threshold = 128;
        [Range(8, 32)] public int cubesPerMarch = 16;
        [Range(1, 5)] public int levelsOfDetail = 3;
        [Range(8, 128)] public int maxChunkSize = 128;
        [SerializeField] private int chunkSize;
        public int ChunkSize => chunkSize;
        [Range(0.1f, 10f)] public float resolution = 1;
        public bool isFiniteSize = true;
        public Vector3Int sizeInChunks;
        [SerializeField] private Vector3 volumeSize;
        public Vector3 VolumeSize => volumeSize;

        void OnValidate() {
            ValidateChunkSize();
            ValidateSizeInChunks();
            ValidateVolumeSize();
        }

        void ValidateChunkSize() {
            chunkSize = cubesPerMarch;
            for (var i = 0; i < cubesPerMarch; ++i) {
                var largerChunkSize = chunkSize * 2;

                if (largerChunkSize > maxChunkSize)
                    break;
                
                chunkSize = largerChunkSize;
            }
        }

        void ValidateVolumeSize() {
            volumeSize = (Vector3)sizeInChunks * chunkSize * resolution;
        }

        void ValidateSizeInChunks() {
            sizeInChunks.x = Mathf.Max(1, sizeInChunks.x);
            sizeInChunks.y = Mathf.Max(1, sizeInChunks.y);
            sizeInChunks.z = Mathf.Max(1, sizeInChunks.z);
        }
    }
}


