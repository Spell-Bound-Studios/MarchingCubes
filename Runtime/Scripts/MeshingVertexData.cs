// Copyright 2025 Spellbound Studio Inc.

using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Spellbound.MarchingCubes {
    /// <summary>
    /// A struct to hold the data every vertex should have
    /// </summary>
    public struct MeshingVertexData {
        public float3 Position;
        public float3 Normal;
        public Color32 Color;

        public MeshingVertexData(float3 position, float3 normal, Color32 color) {
            Position = position;
            Normal = normal;
            Color = color;
        }

        /// <summary>
        /// The memory layout of a single vertex in memory
        /// </summary>
        public static readonly VertexAttributeDescriptor[] VertexBufferMemoryLayout = {
            new(VertexAttribute.Position),
            new(VertexAttribute.Normal),
            new(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4)
        };
    }
}