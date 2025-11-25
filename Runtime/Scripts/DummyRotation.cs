// Copyright 2025 Spellbound Studio Inc.

using UnityEngine;

namespace Spellbound.MarchingCubes {
    public class DummyRotation : MonoBehaviour {
        [SerializeField] private float rotspeed = 50f;
        private void Update() => transform.Rotate(Vector3.up * rotspeed * Time.deltaTime);
    }
}