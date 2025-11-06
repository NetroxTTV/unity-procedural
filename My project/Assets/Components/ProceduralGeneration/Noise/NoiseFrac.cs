using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using VTools.Grid;
using VTools.ScriptableObjectDatabase;
using static FastNoiseLite;

[CreateAssetMenu(menuName = "Procedural Generation Method/NoiseFrac")]
public class NoiseFrac : ProceduralGenerationMethod
{
    [SerializeField] private GameObject playerPrefab;

    [Header("Noise Parameters")]
    [SerializeField] private FastNoiseLite.NoiseType _noisetype = FastNoiseLite.NoiseType.OpenSimplex2;
    [SerializeField][Range(0.02f, 0.05f)] public float Frequency = 0.02f;
    [SerializeField][Range(-1f, 1f)] public float Amplitude = 1f;

    [Header("Fractal Parameters")]
    [SerializeField] private FastNoiseLite.FractalType _fractaltype = FastNoiseLite.FractalType.FBm;
    [SerializeField][Range(1, 8)] public int Octaves = 4;
    [SerializeField][Range(1f, 5f)] public float Lacunarity = 2f;
    [SerializeField][Range(0.1f, 1f)] public float Persistance = 0.5f;

    [Header("Heights")]
    [SerializeField][Range(-1f, 1f)] public float Water_Height = -0.4f;
    [SerializeField][Range(-1f, 1f)] public float Sand_Height = -0.1f;
    [SerializeField][Range(-1f, 1f)] public float Grass_Height = 0.25f;
    [SerializeField][Range(-1f, 1f)] public float Rock_Height = 0.55f;

    private System.Random _random;
    private GenNoise _genNoise;
    private FastNoiseLite _noise;
    private int _seed;

    protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
    {
        _random = RandomService.Random;
        _seed = _random.Next();

        _noise = new FastNoiseLite(_seed);
        _noise.SetNoiseType(_noisetype);
        _noise.SetFrequency(Frequency);
        _noise.SetFractalType(_fractaltype);
        _noise.SetFractalOctaves(Octaves);
        _noise.SetFractalLacunarity(Lacunarity);
        _noise.SetFractalGain(Persistance);

        _genNoise = new GenNoise(Grid, _noise, Amplitude, _random);

        BuildMap();

        if (Grid.TryGetCellByCoordinates(10, 10, out var cell))
        {
            Vector3 spawnPos = new Vector3(cell.GridObject.Cell.Coordinates.x, 0, cell.GridObject.Cell.Coordinates.y);
            Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        }

        await UniTask.Yield(cancellationToken);
    }

    private void BuildMap()
    {
        var waterTemplate = ScriptableObjectDatabase.GetScriptableObject<GridObjectTemplate>(WATER_TILE_NAME);
        var grassTemplate = ScriptableObjectDatabase.GetScriptableObject<GridObjectTemplate>(GRASS_TILE_NAME);
        var mountainsTemplate = ScriptableObjectDatabase.GetScriptableObject<GridObjectTemplate>(ROCK_TILE_NAME);
        var sandTemplate = ScriptableObjectDatabase.GetScriptableObject<GridObjectTemplate>(SAND_TILE_NAME);

        for (int x = 0; x < Grid.Width; x++)
        {
            for (int y = 0; y < Grid.Lenght; y++)
            {
                if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                    continue;

                _genNoise.GenerateNoiseMap(x, y);

                var cellData = _genNoise.GetCell(x, y);
                float value = cellData.NoiseValue;

                GridObjectTemplate template;

                if (value < Water_Height)
                {
                    cellData.SetWater(true);
                    template = waterTemplate;
                }
                else if (value >= Water_Height && value < Sand_Height)
                {
                    cellData.SetSand(true);
                    template = sandTemplate;
                }
                else if (value >= Sand_Height && value < Grass_Height)
                {
                    cellData.SetGrass(true);
                    template = grassTemplate;
                }
                else
                {
                    cellData.SetMountain(true);
                    template = mountainsTemplate;
                }

                GridGenerator.AddGridObjectToCell(cell, template, true);
            }
        }
    }
}

public class FracCells
{
    public bool IsWater { get; private set; }
    public bool IsMountain { get; private set; }
    public bool IsSand { get; private set; }
    public bool IsGrass { get; private set; }
    public bool IsGround => !IsWater && !IsMountain && !IsSand;
    public float NoiseValue { get; private set; }

    public void SetWater(bool isWater) { IsWater = isWater; }
    public void SetGrass(bool isGrass) { IsGrass = isGrass; }
    public void SetMountain(bool isMountain) { IsMountain = isMountain; }
    public void SetSand(bool isSand) { IsSand = isSand; }
    public void SetNoiseValue(float value) { NoiseValue = value; }
}

public class GenNoise
{
    private VTools.Grid.Grid _grid;
    private FastNoiseLite _noise;
    private float _amplitude;
    private System.Random _random;
    private FracCells[,] _cells;

    public GenNoise(VTools.Grid.Grid grid, FastNoiseLite noise, float amplitude, System.Random random)
    {
        _grid = grid;
        _noise = noise;
        _amplitude = amplitude;
        _random = random;
        _cells = new FracCells[grid.Width, grid.Lenght];

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Lenght; y++)
            {
                _cells[x, y] = new FracCells();
            }
        }
    }

    public void GenerateNoiseMap(int x, int y)
    {
        float noiseValue = _noise.GetNoise(x, y) * _amplitude;
        _cells[x, y].SetNoiseValue(noiseValue);
    }

    public FracCells GetCell(int x, int y) => _cells[x, y];
}
