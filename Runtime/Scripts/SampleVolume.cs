// Copyright 2025 Spellbound Studio Inc.

using System.Collections;
using Spellbound.Core;
using Unity.Collections;
using UnityEngine;

namespace Spellbound.MarchingCubes {
    public class SampleVolume : MonoBehaviour, IVolume {
        [SerializeField] private GameObject _chunkPrefab;

        [SerializeField] private Vector3Int cubeSizeInChunks = new(3, 1, 3);

        private NativeList<SparseVoxelData> _data;
        private VoxVolume _voxVolume;

        public VoxVolume VoxelVolume => _voxVolume;

        private void Awake() => _voxVolume = new VoxVolume(this, _chunkPrefab);

        private void Start() {
            if (SingletonManager.TryGetSingletonInstance<MarchingCubesManager>(out var mcManager))
                mcManager.RegisterVoxelVolume(this, mcManager.McConfigBlob.Value.ChunkDataVolumeSize);
            else {
                Debug.LogError("MarchingCubesManager is null.");

                return;
            }

            ManageVolume();
        }

        public void ManageVolume() {
            GenerateSimpleData();
            StartCoroutine(Initialize());
            VoxelVolume.SetLodTarget(Camera.main.transform);
            StartCoroutine(VoxelVolume.ValidateChunkLods());
        }

        private void OnDestroy() {
            if (_data.IsCreated)
                _data.Dispose();

            if (SingletonManager.TryGetSingletonInstance<MarchingCubesManager>(out var mcManager))
                mcManager.UnregisterVoxelVolume(this);
        }

        private void Update() => VoxelVolume.UpdateVolumeOrigin();

        public void GenerateSimpleData() {
            if (!SingletonManager.TryGetSingletonInstance<MarchingCubesManager>(out var mcManager)) {
                Debug.LogError("MarchingCubesManager not found");

                return;
            }

            ref var config = ref mcManager.McConfigBlob.Value;

            var halfChunk = config.ChunkSize / 2;

            _data = new NativeList<SparseVoxelData>(Allocator.Persistent);
            _data.Add(new SparseVoxelData(new VoxelData(byte.MaxValue, MaterialType.Sand), 0));

            _data.Add(new SparseVoxelData(new VoxelData(byte.MaxValue, MaterialType.Dirt),
                config.ChunkDataAreaSize * (halfChunk - 8)));

            _data.Add(new SparseVoxelData(new VoxelData(byte.MinValue, MaterialType.Dirt),
                config.ChunkDataAreaSize * halfChunk));
        }

        public IEnumerator Initialize() {
            var offset = new Vector3Int(
                cubeSizeInChunks.x / 2,
                cubeSizeInChunks.y / 2,
                cubeSizeInChunks.z / 2
            );

            for (var x = 0; x < cubeSizeInChunks.x; x++) {
                for (var y = 0; y < cubeSizeInChunks.y; y++) {
                    for (var z = 0; z < cubeSizeInChunks.z; z++) {
                        var chunkCoord = new Vector3Int(x, y, z) - offset;
                        var chunk = VoxelVolume.RegisterChunk(chunkCoord);
                        chunk.InitializeChunk(_data);

                        yield return null;
                    }
                }
            }
        }
    }
}