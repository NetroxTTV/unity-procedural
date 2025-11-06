using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using VTools.Grid;
using VTools.ScriptableObjectDatabase;

[CreateAssetMenu(menuName = "Procedural Generation Method/CellularAutomata")]
public class CellularAutomata : ProceduralGenerationMethod
{
    [SerializeField] private int maxIterations = 4;
    [SerializeField] private int noise_density = 45;
    [SerializeField] private int mountainThreshold = 6;
    [SerializeField] private GameObject playerPrefab;

    private System.Random _random;
    private Automata _automata;

    protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
    {
        _random = RandomService.Random;
        _automata = new Automata(Grid, noise_density, _random);
        _automata.SetupArea();

        for (int i = 0; i < maxIterations; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _automata.CellularStep();
            BuildMap();
            await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
        }

        _automata.GenerateMountains(mountainThreshold);
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
        GridObjectTemplate template;

        for (int x = 0; x < Grid.Width; x++)
        {
            for (int z = 0; z < Grid.Lenght; z++)
            {
                if (!Grid.TryGetCellByCoordinates(x, z, out var cell))
                    continue;

                var cellData = _automata.GetCell(x, z);

                if (cellData.IsWater)
                {
                    template = waterTemplate;
                }
                else if (cellData.IsMountain)
                {
                    template = mountainsTemplate;
                }
                else
                {
                    template = grassTemplate;
                }
                GridGenerator.AddGridObjectToCell(cell, template, true);
            }
        }
    }
}

public class Cells
{
    public bool IsWater { get; private set; }
    public bool IsMountain { get; private set; }
    public bool IsGround => !IsWater && !IsMountain;

    public void SetWater(bool isWater) { IsWater = isWater; }
    public void SetMountain(bool isMountain) { IsMountain = isMountain; }
}

public class Automata
{
    private VTools.Grid.Grid _grid;
    private int _noiseDensity;
    private System.Random _random;
    private Cells[,] _cells;
    private Cells[,] _nextCells;

    public Automata(VTools.Grid.Grid grid, int noiseDensity, System.Random random)
    {
        _grid = grid;
        _noiseDensity = noiseDensity;
        _random = random;
        _cells = new Cells[grid.Width, grid.Lenght];
        _nextCells = new Cells[grid.Width, grid.Lenght];

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Lenght; y++)
            {
                _cells[x, y] = new Cells();
                _nextCells[x, y] = new Cells();
            }
        }
    }

    public void SetupArea()
    {
        for (int x = 0; x < _grid.Width; x++)
        {
            for (int y = 0; y < _grid.Lenght; y++)
            {
                bool isWater = _random.Next(0, 100) < _noiseDensity;
                _cells[x, y].SetWater(isWater);
            }
        }
    }

    public void CellularStep()
    {
        for (int x = 0; x < _grid.Width; x++)
        {
            for (int y = 0; y < _grid.Lenght; y++)
            {
                int waterNeighbors = CountWaterNeighbors(x, y);

                if (waterNeighbors > 4)
                    _nextCells[x, y].SetWater(true);
                else if (waterNeighbors < 4)
                    _nextCells[x, y].SetWater(false);
                else
                    _nextCells[x, y].SetWater(_cells[x, y].IsWater);
            }
        }

        var temp = _cells;
        _cells = _nextCells;
        _nextCells = temp;
    }

    public void GenerateMountains(int threshold)
    {
        bool[,] shouldBeMountain = new bool[_grid.Width, _grid.Lenght];

        for (int x = 0; x < _grid.Width; x++)
        {
            for (int y = 0; y < _grid.Lenght; y++)
            {
                if (_cells[x, y].IsGround)
                {
                    int groundNeighbors = CountGroundNeighbors(x, y);

                    if (groundNeighbors >= threshold)
                    {
                        shouldBeMountain[x, y] = true;
                    }
                }
            }
        }

        for (int x = 0; x < _grid.Width; x++)
        {
            for (int y = 0; y < _grid.Lenght; y++)
            {
                if (shouldBeMountain[x, y])
                {
                    _cells[x, y].SetMountain(true);
                }
            }
        }
    }

    private int CountWaterNeighbors(int cellX, int cellY)
    {
        int count = 0;

        for (int nx = cellX - 1; nx <= cellX + 1; nx++)
        {
            for (int ny = cellY - 1; ny <= cellY + 1; ny++)
            {
                if (nx == cellX && ny == cellY)
                    continue;

                if (nx < 0 || nx >= _grid.Width || ny < 0 || ny >= _grid.Lenght)
                {
                    count++;
                    continue;
                }

                if (_cells[nx, ny].IsWater)
                    count++;
            }
        }

        return count;
    }

    private int CountGroundNeighbors(int cellX, int cellY)
    {
        int count = 0;

        for (int nx = cellX - 1; nx <= cellX + 1; nx++)
        {
            for (int ny = cellY - 1; ny <= cellY + 1; ny++)
            {
                if (nx == cellX && ny == cellY)
                    continue;

                if (nx < 0 || nx >= _grid.Width || ny < 0 || ny >= _grid.Lenght)
                    continue;

                if (_cells[nx, ny].IsGround)
                    count++;
            }
        }

        return count;
    }

    public Cells GetCell(int x, int y) { return _cells[x, y]; }
}