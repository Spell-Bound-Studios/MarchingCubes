// Copyright 2025 Spellbound Studio Inc.

using UnityEngine;

namespace Spellbound.MarchingCubes {
    public class CustomVolume : BasicVolume {
        protected override void Awake() {
            base.Awake();
            Debug.Log("I overrode Awake");
        }
    }
}