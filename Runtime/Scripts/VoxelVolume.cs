// Copyright 2025 Spellbound Studio Inc.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Spellbound.Core;
using Unity.Collections;
using UnityEngine;

namespace Spellbound.MarchingCubes {
    public class VoxelVolume : MonoBehaviour, IVoxelVolume {
        private Dictionary<Vector3Int, IVoxelTerrainChunk> _chunkDict = new();
        [SerializeField] private GameObject _chunkPrefab;

        [SerializeField] private Vector3Int cubeSizeInChunks = new(3, 1, 3);

        private NativeList<SparseVoxelData> _dummyData;

        public IVoxelTerrainChunk GetChunkByVoxelPosition(Vector3Int voxelPos) {
            ref var config = ref SingletonManager.GetSingletonInstance<MarchingCubesManager>().McConfigBlob.Value;
            var coord = McStaticHelper.VoxelToChunk(voxelPos, config.ChunkSize);
            return GetChunkByCoord(coord);
        }
        
        public IVoxelTerrainChunk GetChunkByWorldPosition(Vector3 worldPos) {
            ref var config = ref SingletonManager.GetSingletonInstance<MarchingCubesManager>().McConfigBlob.Value;
    
            // Convert world to volume-local space
            var localPos = transform.InverseTransformPoint(worldPos);
    
            // Convert to voxel coordinates
            var voxelPos = new Vector3Int(
                Mathf.FloorToInt(localPos.x / config.Resolution),
                Mathf.FloorToInt(localPos.y / config.Resolution),
                Mathf.FloorToInt(localPos.z / config.Resolution)
            );
    
            return GetChunkByVoxelPosition(voxelPos);
        }

        public IVoxelTerrainChunk GetChunkByCoord(Vector3Int coord) => _chunkDict.GetValueOrDefault(coord);

        public Vector3Int WorldToVoxelSpace(Vector3 worldPosition) {
            ref var config = ref SingletonManager.GetSingletonInstance<MarchingCubesManager>().McConfigBlob.Value;
            
            var localPos = transform.InverseTransformPoint(worldPosition);
            
            return new Vector3Int(
                Mathf.FloorToInt(localPos.x / config.Resolution),
                Mathf.FloorToInt(localPos.y / config.Resolution),
                Mathf.FloorToInt(localPos.z / config.Resolution)
            );
        }
        
        private void Start() {
            if (SingletonManager.TryGetSingletonInstance<MarchingCubesManager>(out var mcManager)) {
                mcManager.RegisterVoxelVolume(this, mcManager.McConfigBlob.Value.ChunkDataVolumeSize);
            }
            else {
                Debug.LogError("MarchingCubesManager is null.");

                return;
            }
            GenerateSimpleData();
            StartCoroutine(Initialize());
            StartCoroutine(ValidateChunkLods());
        }

        private void OnDestroy() {
            if (_dummyData.IsCreated) 
                _dummyData.Dispose();
            
            if (SingletonManager.TryGetSingletonInstance<MarchingCubesManager>(out var mcManager)) {
                mcManager.UnregisterVoxelVolume(this);
            }
        }
        
        public Transform GetTransform()  => transform;

        public void GenerateSimpleData() {
            if (!SingletonManager.TryGetSingletonInstance<MarchingCubesManager>(out var mcManager)) {
                Debug.LogError("MarchingCubesManager not found");

                return;
            }

            ref var config = ref mcManager.McConfigBlob.Value;

            var halfChunk = config.ChunkSize / 2;

            _dummyData = new NativeList<SparseVoxelData>(Allocator.Persistent);
            _dummyData.Add(new SparseVoxelData(new VoxelData(byte.MaxValue, MaterialType.Sand), 0));

            _dummyData.Add(new SparseVoxelData(new VoxelData(byte.MaxValue, MaterialType.Dirt),
                config.ChunkDataAreaSize * (halfChunk - 8)));

            _dummyData.Add(new SparseVoxelData(new VoxelData(byte.MinValue, MaterialType.Dirt),
                config.ChunkDataAreaSize * halfChunk));
        }

        private IEnumerator Initialize() {
            // Calculate offset to center the volume
            var offset = new Vector3Int(
                cubeSizeInChunks.x / 2,
                cubeSizeInChunks.y / 2,
                cubeSizeInChunks.z / 2
            );

            for (var x = 0; x < cubeSizeInChunks.x; x++) {
                for (var y = 0; y < cubeSizeInChunks.y; y++) {
                    for (var z = 0; z < cubeSizeInChunks.z; z++) {
                        // Subtract offset to center around origin
                        var chunkCoord = new Vector3Int(x, y, z) - offset;
                        var chunk = RegisterChunk(chunkCoord);
                        chunk.InitializeVoxelData(_dummyData);

                        yield return null;
                    }
                }
            }
        }

        private IVoxelTerrainChunk RegisterChunk(Vector3Int chunkCoord) {
            var newChunk = CreateNChunk(chunkCoord);
            _chunkDict[chunkCoord] = newChunk;
            //OnChunkCountChanged?.Invoke(ClientChunkDict.Count);

            return newChunk;
        }

        private IVoxelTerrainChunk CreateNChunk(Vector3Int chunkCoord) {
            ref var config = ref SingletonManager.GetSingletonInstance<MarchingCubesManager>().McConfigBlob.Value;

            var localChunkPos = (Vector3)(chunkCoord * config.ChunkSizeResolution);
            var worldChunkPos = transform.TransformPoint(localChunkPos);

            var chunkObj = Instantiate(
                _chunkPrefab,
                worldChunkPos,
                transform.rotation,
                transform
            );

            if (!chunkObj.TryGetComponent(out IVoxelTerrainChunk chunk)) return null;

            chunk.SetChunkFields(chunkCoord);

            return chunk;
        }

        private IEnumerator ValidateChunkLods() {
            while (true) {
                var chunkList = new List<Vector3Int>(_chunkDict.Keys.ToList());

                foreach (var coord in chunkList) {
                    if (!_chunkDict.TryGetValue(coord, out var chunk))
                        continue;

                    if (!chunk.HasVoxelData())
                        continue;

                    if (!SingletonManager.TryGetSingletonInstance<MarchingCubesManager>(out _))
                        continue;

                    // playerPosition is in world space for LOD distance calculations
                    chunk.ValidateOctreeLods(Camera.main.transform.position);

                    yield return null;
                }

                yield return null;
            }
        }
    }
}