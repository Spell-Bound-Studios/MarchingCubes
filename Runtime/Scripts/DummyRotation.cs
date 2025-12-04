// Copyright 2025 Spellbound Studio Inc.

using UnityEngine;

namespace Spellbound.MarchingCubes {
    public class DummyRotation : MonoBehaviour {
        [SerializeField] private Vector3 rotaxis = Vector3.up;
        [SerializeField] private float rotspeed = 50f;
        private void Update() => transform.Rotate(rotaxis * rotspeed * Time.deltaTime);
    }
}