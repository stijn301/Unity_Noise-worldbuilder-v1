using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
  [SerializeField] Transform vegetationPropsHolder;
  [SerializeField] GameObject meshHolder;
  
  public Transform PropHolder => vegetationPropsHolder;

  public Coord chunkCoord {get; private set;}

  // data maps
  float[,] heightMap;
  float[,] temperatureMap;
  float[,] humidityMap;
  public int[,] biomeMap {get; private set;}

  public bool hasVegetation {get; private set;} = false;
  public VegetationInChunk[] vegetationInChunk {get; private set;}

  public bool hasBiomeTexture {get; private set;} = false;
  Texture2D biomeTexture;

  ChunksManager manager;
  MeshFilter meshFilter;
  MeshRenderer meshRenderer;
  MeshCollider meshCollider;

  // water layer mesh
  public bool hasWaterLayer {get; private set;} = false;
  public MeshFilter waterLayerMeshFilter {get; private set;}

  public void Init(ChunksManager chunksManager, Coord chunkCoord, float[,] heightMap, float[,] temperatureMap, float[,] humidityMap) {
    manager = chunksManager;
    this.chunkCoord = chunkCoord;
    this.heightMap = heightMap;
    this.temperatureMap = temperatureMap;
    this.humidityMap = humidityMap;

    meshFilter = meshHolder.GetComponent<MeshFilter>();
    meshRenderer = meshHolder.GetComponent<MeshRenderer>();
    meshCollider = meshHolder.GetComponent<MeshCollider>();

    meshRenderer.material.SetFloat("_Glossiness", 0);
    CreateBiomeMap();
    manager.CheckIfWaterInChunk(this);
  }

  // accessors
  public float GetHeightMapAtCoord(Coord tileCoord) {
    return heightMap[tileCoord.x, tileCoord.y];
  }

  public int GetBiomeIdAtCoord(Coord tileCoord) {
    return biomeMap[tileCoord.x, tileCoord.y];
  }

  // creating & drawing map data
  void CreateBiomeMap() {
    biomeMap = BiomeGenerator.GenerateBiomeMap(heightMap, temperatureMap, humidityMap, manager.GetBiomeSet());
  }

  public void CreateFlatMesh(int meshSize, float tileSize) {
    MeshData meshData = MeshGenerator.GenerateFlatMesh(meshSize, tileSize);

    meshFilter.mesh = meshData.CreateMesh(true);
  }

  public void CreateMesh(float tileSize, float heightMultiplier, AnimationCurve heightCurve) {
    MeshData meshData = MeshGenerator.GenerateMesh(heightMap, tileSize, heightMultiplier, heightCurve);
    Mesh mesh = meshData.CreateMesh(true);
    meshFilter.mesh = mesh;
    meshCollider.sharedMesh = mesh;
  }

  public void OnDrawMap(MapToDraw mapToDraw) {
    if(mapToDraw == MapToDraw.Biome) {
      if(hasBiomeTexture) {
        meshRenderer.material.mainTexture = biomeTexture;
      }

    } else if(mapToDraw == MapToDraw.Height) {
      meshRenderer.material.mainTexture = TextureGenerator.GenerateFromNoiseMap(heightMap);

    } else if(mapToDraw == MapToDraw.Temperature) {
      meshRenderer.material.mainTexture = TextureGenerator.GenerateFromNoiseMap(temperatureMap, Color.blue, Color.red);

    } else if(mapToDraw == MapToDraw.Humidity) {
      meshRenderer.material.mainTexture = TextureGenerator.GenerateFromNoiseMap(humidityMap, Color.white, Color.blue);
    }
  }

  public void SetTerrain(Color[] groundColorArray) {
    biomeTexture = TextureGenerator.GenerateFromColorArray(
      biomeMap.GetLength(0),
      manager.CalculateGroundColors(chunkCoord, biomeMap)
    );
    hasBiomeTexture = true;
  }

  public void SetVegetation(VegetationInChunk[] vegetation) {
    vegetationInChunk = vegetation;
    hasVegetation = true;
  }

  public void SetWaterLayer(MeshFilter meshFilter) {
    waterLayerMeshFilter = meshFilter;
    hasWaterLayer = true;
  }

  // biome texture

  void TryCreateBiomeTexture() {
    if(manager.CheckIfChunkCanCreateTerrain(chunkCoord)) {
      biomeTexture = TextureGenerator.GenerateFromColorArray(
        biomeMap.GetLength(0),
        manager.CalculateGroundColors(chunkCoord, biomeMap)
      );
      meshRenderer.material.mainTexture = biomeTexture;
      hasBiomeTexture = true;
    }
  }

  void CheckBiomeTexture(MapToDraw mapToDraw) {
    if(mapToDraw == MapToDraw.Biome && !hasBiomeTexture) {
      TryCreateBiomeTexture();
    }
  }

  // chunk updates
  public void SetChunkActive(bool value) {
    if(meshHolder.activeSelf != value) {
      meshHolder.SetActive(value);
      if(hasWaterLayer) {
        waterLayerMeshFilter.gameObject.SetActive(value);
      }

      if(value) {
        manager.OnDrawMap += OnDrawMap;
        manager.OnVisibleChunksUpdate += CheckBiomeTexture;

      } else {
        manager.OnDrawMap -= OnDrawMap;
        manager.OnVisibleChunksUpdate -= CheckBiomeTexture;
      }
    }
  }

  public void SetVegetationActive(bool value) {
    if(vegetationPropsHolder.gameObject.activeSelf != value) {
      if(value && !hasVegetation) {
        manager.CreateVegetationForChunk(this);
      }
      vegetationPropsHolder.gameObject.SetActive(value);
    }
  }

  public void ToggleTerrain(bool isVisible) {
    
  }

  public void ToggleVegetation(bool isVisible) {
    
  }

  public void ToggleWater(bool isVisible) {

  }


  // THIS IS REALLY TEMPORARY THOUGH
  // TODO: remove this update function, this should move to ChunksManager, but ChunksManager & WorldBuilder need some reworks & refactoring!

  void Update() {
    if(hasWaterLayer) {
      manager.UpdateChunkWaterLayer(waterLayerMeshFilter, chunkCoord);
    }
  }
}

public struct VegetationInChunk {
  public Coord tileCoord {get; private set;}
  public Vector3 posOffset {get; private set;}
  public int angle {get; private set;}
  public bool placeWithRaycast {get; private set;}
  public GameObject prefab {get; private set;}

  public VegetationInChunk(Coord tileCoord, Vector3 posOffset, int angle, bool placeWithRaycast, GameObject prefab) {
    this.tileCoord = tileCoord;
    this.posOffset = posOffset;
    this.angle = angle;
    this.placeWithRaycast = placeWithRaycast;
    this.prefab = prefab;
  }
}