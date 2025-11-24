// Copyright 2025 Spellbound Studio Inc.

using System;
using System.Collections.Generic;
using Spellbound.Core;
using Unity.Collections;
using UnityEngine;

namespace Spellbound.MarchingCubes {
    public class TerrainChunk : MonoBehaviour, IVoxelTerrainChunk {
        private Vector3Int _chunkCoord;
        private BoundsInt _bounds;
        private NativeList<SparseVoxelData> _sparseVoxels;
        private Dictionary<int, VoxelEdit> _voxelEdits;
        private OctreeNode _rootNode;
        private DensityRange _densityRange;
        private MarchingCubesManager _mcManager;
        private IVoxelVolume _chunkManager;

        public void Update() {
            if (_rootNode == null) {
                return;
            }
            _rootNode.ValidateMaterial();
        }

        public NativeArray<VoxelData> GetVoxelDataArray() =>
                _mcManager.GetOrUnpackVoxelArray(_chunkCoord, this, _sparseVoxels);

        public DensityRange GetDensityRange() => _densityRange;

        public void SetDensityRange(DensityRange densityRange) => _densityRange = densityRange;
        public Vector3Int GetChunkCoord() => _chunkCoord;

        public Transform GetChunkTransform() => transform;

        public void InitializeVoxelData(NativeList<SparseVoxelData> voxels) {
            if (_sparseVoxels.IsCreated)
                Debug.LogError($"_sparseVoxels is already created for this chunkCoord {_chunkCoord}.");

            if (!voxels.IsCreated) {
                Debug.LogError(
                    $"_sparseVoxels being initialized with a List is already created for this chunkCoord {_chunkCoord}.");
            }

            _sparseVoxels = new NativeList<SparseVoxelData>(voxels.Length, Allocator.Persistent);
            _sparseVoxels.AddRange(voxels.AsArray());
            _rootNode = new OctreeNode(Vector3Int.zero, _mcManager.McConfigBlob.Value.LevelsOfDetail, this, _chunkManager.GetTransform());
            if (Camera.main != null) ValidateOctreeLods(Camera.main.transform.position);
        }

        public void UpdateVoxelData(NativeList<SparseVoxelData> voxels, DensityRange densityRange) {
            if (!_sparseVoxels.IsCreated)
                return;

            _sparseVoxels.Clear();
            _sparseVoxels.CopyFrom(voxels);
            _densityRange = densityRange;
        }

        public void BroadcastNewLeafAcrossChunks(OctreeNode newLeaf, Vector3Int pos, int index) {
            ref var config = ref _mcManager.McConfigBlob.Value;

            var worldVoxelPos = pos + _chunkCoord * config.ChunkSize;

            if (_bounds.Contains(worldVoxelPos)) {
                _rootNode?.ValidateTransition(newLeaf, pos, McStaticHelper.GetTransitionFaceMask(index));
                return;
            }

            var neighborCoord = McStaticHelper.GetNeighborCoord(index, _chunkCoord);
            var neighborChunk = _chunkManager.GetChunkByCoord(neighborCoord);

            if (neighborChunk == null)
                return;
            
            var neighborLocalPos = worldVoxelPos - neighborCoord * config.ChunkSize;
            neighborChunk.BroadcastNewLeafAcrossChunks(newLeaf, neighborLocalPos, index);
        }

        public void AddToVoxelEdits(List<VoxelEdit> newVoxelEdits) {
            if (newVoxelEdits.Count == 0)
                return;

            var voxelArray = GetVoxelDataArray();
            var hasAnyEdits = false;
            BoundsInt editBounds = default;

            ref var config = ref _mcManager.McConfigBlob.Value;

            foreach (var voxelEdit in newVoxelEdits) {
                var index = voxelEdit.index;
                var existingVoxel = voxelArray[index];
                
                if (voxelEdit.density == existingVoxel.Density &&
                    voxelEdit.MaterialType == existingVoxel.MaterialType)
                    continue;

                voxelArray[index] = new VoxelData(voxelEdit.density, voxelEdit.MaterialType);

                McStaticHelper.IndexToInt3(index, config.ChunkDataAreaSize, config.ChunkDataWidthSize, out var x, out var y, out var z);
                var voxelPos = new Vector3Int(x, y, z);

                if (!hasAnyEdits) {
                    editBounds = new BoundsInt(voxelPos, Vector3Int.one);
                    hasAnyEdits = true;
                }
                else {
                    var min = Vector3Int.Min(editBounds.min, voxelPos);
                    var max = Vector3Int.Max(editBounds.max, voxelPos + Vector3Int.one);
                    editBounds = new BoundsInt(min, max - min);
                }

                _densityRange.Encapsulate(voxelEdit.density);
            }

            if (hasAnyEdits) ValidateOctreeEdits(editBounds);

            _mcManager.PackVoxelArray();
            _mcManager.CompleteAndApplyMarchingCubesJobs();
            _mcManager.ReleaseVoxelArray();
        }

        public VoxelData GetVoxelData(int index) {
            ref var config = ref _mcManager.McConfigBlob.Value;
            var sparseIndex = McStaticHelper.BinarySearchVoxelData(index, config.ChunkDataVolumeSize, _sparseVoxels);

            return _sparseVoxels[sparseIndex].Voxel;
        }

        public VoxelData GetVoxelDataFromVoxelPosition(Vector3Int position) {
            ref var config = ref _mcManager.McConfigBlob.Value;
            var chunkSpacePosition = position - _chunkCoord * config.ChunkSize;
            
            var index = McStaticHelper.Coord3DToIndex(
                chunkSpacePosition.x, 
                chunkSpacePosition.y, 
                chunkSpacePosition.z,
                config.ChunkDataAreaSize,
                config.ChunkDataWidthSize
            );

            return GetVoxelData(index);
        }

        public bool HasVoxelData() => _sparseVoxels.IsCreated;

        // TODO: Null checking twice is weird.
        public void ValidateOctreeEdits(BoundsInt bounds) {
            _rootNode?.ValidateOctreeEdits(bounds, GetVoxelDataArray());
        }

        public void ValidateOctreeLods(Vector3 playerPosition) {
            if (!_sparseVoxels.IsCreated)
                return;

            _rootNode.ValidateOctreeLods(playerPosition, GetVoxelDataArray());
            _mcManager.CompleteAndApplyMarchingCubesJobs();
            _mcManager.ReleaseVoxelArray();
        }

        private void OnDrawGizmos() {
            if (_mcManager == null) return;
            ref var config = ref _mcManager.McConfigBlob.Value;
    
            var worldSize = (Vector3)_bounds.size * config.Resolution;
            var localOffset = worldSize * 0.5f;
            var worldCenter = transform.position + transform.TransformDirection(localOffset);

            Gizmos.DrawWireCube(worldCenter, worldSize);
        }

        public void SetChunkFields(Vector3Int coord) {
            _mcManager = SingletonManager.GetSingletonInstance<MarchingCubesManager>();
            ref var config = ref _mcManager.McConfigBlob.Value;
            _chunkManager = GetComponentInParent<IVoxelVolume>();
            _chunkCoord = coord;

            var voxelMin = coord * config.ChunkSize;
            _bounds = new BoundsInt(voxelMin, config.ChunkSize * Vector3Int.one);
            gameObject.name = coord.ToString();
        }

        private void OnDestroy() {
            _rootNode?.Dispose();

            if (_sparseVoxels.IsCreated)
                _sparseVoxels.Dispose();
        }
    }
}