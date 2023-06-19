using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Examples.Odin
{
    public class AssetOnlyExample: MonoBehaviour
    {
        [AssetsOnly]
        public List<GameObject> OnlyPrefabs;
        
        [AssetsOnly]
        public GameObject SomePrefab;
        
        [AssetsOnly]
        public Material MaterialAsset;
        
        [AssetsOnly]
        public MeshRenderer SomeMeshRendererOnPrefab;
        
        [SceneObjectsOnly]
        public List<GameObject> OnlySceneObjects;
        
        [SceneObjectsOnly]
        public GameObject SomeSceneObject;
        
        [SceneObjectsOnly]
        public MeshRenderer SomeMeshRenderer;
    }
}