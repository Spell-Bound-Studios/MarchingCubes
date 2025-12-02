// Copyright 2025 Spellbound Studio Inc.

using Spellbound.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spellbound.MarchingCubes {
    [CreateAssetMenu(menuName = "Spellbound/MarchingCubes/DataFactory/SimpleShapesDataFactory")]
    public class SimpleShapesDataFactory : DataFactory {
        public override void FillDataArray(
            Vector3Int chunkCoord,
            BlobAssetReference<VolumeConfigBlobAsset> configBlob,
            NativeArray<VoxelData> data) {
            if (!SingletonManager.TryGetSingletonInstance<MarchingCubesManager>(out var mcManager)) {
                Debug.LogError("MarchingCubesManager not found");
                return;
            }
            var sandIndex = mcManager.materialDatabase.GetMaterialIndex("Sand");
            var sandVoxel = new VoxelData(byte.MaxValue, sandIndex);
            for (var i = 0; i < data.Length; ++i) {
                data[i] = sandVoxel;
            }
        }
    }
}