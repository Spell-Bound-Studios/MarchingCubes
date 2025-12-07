// Copyright 2025 Spellbound Studio Inc.

using System.Collections.Generic;
using System.Linq;
using Spellbound.Core;
using Spellbound.Core.Console;
using UnityEngine;

namespace Spellbound.MarchingCubes {
    public static class SbVoxel {
        public static bool IsInitialized() =>
                SingletonManager.TryGetSingletonInstance<MarchingCubesManager>(out _)
                && SingletonManager.TryGetSingletonInstance<IVolume>(out _);

        public static bool IsActive() {
            var mcManager = SingletonManager.GetSingletonInstance<MarchingCubesManager>();

            return mcManager.IsActive();
        }

        public static bool IsInsideTerrain(Vector3 position) {
            var mcManager = SingletonManager.GetSingletonInstance<MarchingCubesManager>();

            //TODO MAGIC NUMBER
            return mcManager.QueryVoxel(position).Density >= 128;
        }

        // Updated SbVoxel.cs - showing the changed methods

        public static void RemoveSphere(Vector3 position, float radius = 3, int delta = byte.MaxValue) {
            if (!SingletonManager.TryGetSingletonInstance<MarchingCubesManager>(out var mcManager)) {
                Debug.LogError("MarchingCubesManager not found");
                return;
            }

            var diggableMaterials = new List<byte> { 0, 1, 2 };
            mcManager.ExecuteTerraform(
                volume => TerraformCommands.RemoveSphere(volume, position, 3f, 1),
                diggableMaterials.ToHashSet()
            );
        }

        [ConsoleUtilityCommand("AddSphere", "Positive terraform with 2 radius")]
        public static void AddSphere(Vector3 position, IVolume hitVolume, float radius = 2f, int delta = 255) {
            if (!SingletonManager.TryGetSingletonInstance<MarchingCubesManager>(out var mcManager)) {
                Debug.LogError("MarchingCubesManager not found");
                return;
            }
    
            mcManager.ExecuteTerraform(
                volume => TerraformCommands.AddSphere(volume, position, 3, radius, delta),
                null,
                hitVolume
            );
        }
    }
}