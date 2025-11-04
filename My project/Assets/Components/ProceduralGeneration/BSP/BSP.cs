using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using VTools.Grid;
using VTools.ScriptableObjectDatabase;
using VTools.Utility;

[CreateAssetMenu(menuName = "Procedural Generation Method/BSP")]
public class BSP : ProceduralGenerationMethod
{
    [SerializeField] private int maxIterations = 4;
    [SerializeField] private int minRoomSize = 4;
    [SerializeField] private int roomPadding = 2;

    protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
    {
        RectInt allGrid = new RectInt(0, 0, Grid.Width, Grid.Lenght);
        var root = new TestBSP(Grid, allGrid, maxIterations, minRoomSize);
        root.DivideGrid();

        PlaceRoomsInLeaves(root);
        CreateCorridors(root);

        BuildGround();
        await UniTask.Yield(cancellationToken);
    }

    private void BuildGround()
    {
        var groundTemplate = ScriptableObjectDatabase.GetScriptableObject<GridObjectTemplate>("Grass");

        for (int x = 0; x < Grid.Width; x++)
        {
            for (int z = 0; z < Grid.Lenght; z++)
            {
                if (!Grid.TryGetCellByCoordinates(x, z, out var chosenCell))
                {
                    Debug.LogError($"Unable to get cell on coordinates : ({x}, {z})");
                    continue;
                }

                GridGenerator.AddGridObjectToCell(chosenCell, groundTemplate, false);
            }
        }
    }

    private void PlaceRoomsInLeaves(TestBSP node)
    {
        if (node.IsLeaf())
        {
            node.CreateRoom(roomPadding);
            RectInt room = node.GetRoom();

            Debug.Log($"Room created at {room}");

            for (int x = room.xMin; x < room.xMax; x++)
            {
                for (int y = room.yMin; y < room.yMax; y++)
                {
                    PlaceCell(x, y);
                }
            }
        }
        else
        {
            if (node.GetChild1() != null)
                PlaceRoomsInLeaves(node.GetChild1());
            if (node.GetChild2() != null)
                PlaceRoomsInLeaves(node.GetChild2());
        }
    }

    private void PlaceCell(int x, int y)
    {
        if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
            return;

        AddTileToCell(cell, ROOM_TILE_NAME, true);
    }

    private void CreateCorridors(TestBSP node)
    {
        if (node.IsLeaf())
            return;

        if (node.GetChild1() != null)
            CreateCorridors(node.GetChild1());
        if (node.GetChild2() != null)
            CreateCorridors(node.GetChild2());

        ConnectRooms(node.GetChild1().GetCenter(), node.GetChild2().GetBounds().GetCenter());
    }

    private void ConnectRooms(Vector2Int room1Center, Vector2Int room2Center)
    {
        if (Random.value > 0.5f)
        {
            CreateHorizontalCorridor(room1Center.x, room2Center.x, room1Center.y);
            CreateVerticalCorridor(room1Center.y, room2Center.y, room2Center.x);
        }
        else
        {
            CreateVerticalCorridor(room1Center.y, room2Center.y, room1Center.x);
            CreateHorizontalCorridor(room1Center.x, room2Center.x, room2Center.y);
        }
    }

    private void CreateDogLegCorridor(Vector2Int start, Vector2Int end)
    {
        bool horizontalFirst = RandomService.Chance(0.5f);

        if (horizontalFirst)
        {
            // Draw horizontal line first, then vertical
            CreateHorizontalCorridor(start.x, end.x, start.y);
            CreateVerticalCorridor(start.y, end.y, end.x);
        }
        else
        {
            // Draw vertical line first, then horizontal
            CreateVerticalCorridor(start.y, end.y, start.x);
            CreateHorizontalCorridor(start.x, end.x, end.y);
        }
    }

    /// Creates a horizontal corridor from x1 to x2 at the given y coordinate
    private void CreateHorizontalCorridor(int x1, int x2, int y)
    {
        int xMin = Mathf.Min(x1, x2);
        int xMax = Mathf.Max(x1, x2);

        for (int x = xMin; x <= xMax; x++)
        {
            if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                continue;

            AddTileToCell(cell, CORRIDOR_TILE_NAME, true);
        }
    }

    /// Creates a vertical corridor from y1 to y2 at the given x coordinate
    private void CreateVerticalCorridor(int y1, int y2, int x)
    {
        int yMin = Mathf.Min(y1, y2);
        int yMax = Mathf.Max(y1, y2);

        for (int y = yMin; y <= yMax; y++)
        {
            if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                continue;

            AddTileToCell(cell, CORRIDOR_TILE_NAME, true);
        }
    }

}

public class TestBSP
{
    private VTools.Grid.Grid _grid;
    private RectInt _bounds;
    private RectInt _room;
    private TestBSP _child1, _child2;
    private int _currentIteration;
    private int _maxIterations;
    private int _minRoomSize;

    public TestBSP(VTools.Grid.Grid grid, RectInt bounds, int maxIterations, int minRoomSize, int currentIteration = 0)
    {
        _grid = grid;
        _bounds = bounds;
        _maxIterations = maxIterations;
        _minRoomSize = minRoomSize;
        _currentIteration = currentIteration;
    }

    public void CreateRoom(int padding)
    {
        int roomWidth = Random.Range(_minRoomSize, _bounds.width - padding * 2 + 1);
        int roomHeight = Random.Range(_minRoomSize, _bounds.height - padding * 2 + 1);

        int roomX = Random.Range(_bounds.xMin + padding, _bounds.xMax - roomWidth - padding + 1);
        int roomY = Random.Range(_bounds.yMin + padding, _bounds.yMax - roomHeight - padding + 1);

        _room = new RectInt(roomX, roomY, roomWidth, roomHeight);
    }

    public void DivideGrid()
    {
        if (_currentIteration >= _maxIterations)
        {
            return;
        }

        if (!CanSplit())
        {
            return;
        }

        CreateChilds();

        _child1.DivideGrid();
        _child2.DivideGrid();
    }

    private bool CanSplit()
    {
        return _bounds.width >= _minRoomSize * 2 && _bounds.height >= _minRoomSize * 2;
    }

    private void CreateChilds()
    {
        bool splitHorizontally = Random.value > 0.5f;

        if (_bounds.width > _bounds.height)
        {
            splitHorizontally = false;
        }
        else if (_bounds.height > _bounds.width)
        {
            splitHorizontally = true;
        }

        if (splitHorizontally)
        {
            int splitY = Random.Range(_bounds.yMin + _minRoomSize, _bounds.yMax - _minRoomSize);

            _child1 = new TestBSP(
                _grid,
                new RectInt(_bounds.x, _bounds.y, _bounds.width, splitY - _bounds.y),
                _maxIterations,
                _minRoomSize,
                _currentIteration + 1
            );

            _child2 = new TestBSP(
                _grid,
                new RectInt(_bounds.x, splitY, _bounds.width, _bounds.yMax - splitY),
                _maxIterations,
                _minRoomSize,
                _currentIteration + 1
            );
        }
        else
        {
            int splitX = Random.Range(_bounds.xMin + _minRoomSize, _bounds.xMax - _minRoomSize);

            _child1 = new TestBSP(
                _grid,
                new RectInt(_bounds.x, _bounds.y, splitX - _bounds.x, _bounds.height),
                _maxIterations,
                _minRoomSize,
                _currentIteration + 1
            );

            _child2 = new TestBSP(
                _grid,
                new RectInt(splitX, _bounds.y, _bounds.xMax - splitX, _bounds.height),
                _maxIterations,
                _minRoomSize,
                _currentIteration + 1
            );
        }
    }

    public VTools.Grid.Grid GetGrid() { return _grid; }
    public bool IsLeaf() { return _child1 == null && _child2 == null; }
    public TestBSP GetChild1() { return _child1; }
    public TestBSP GetChild2() { return _child2; }
    public RectInt GetBounds() { return _bounds; }
    public RectInt GetRoom() { return _room; }
    public Vector2Int GetCenter() { return new Vector2Int(_bounds.x / 2 , _bounds.y / 2 ); }
}