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

    private System.Random _random;

    protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
    {
        _random = RandomService.Random;

        RectInt allGrid = new RectInt(0, 0, Grid.Width, Grid.Lenght);
        var root = new TestBSP(Grid, allGrid, maxIterations, minRoomSize, _random);
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

        ConnectRooms(node.GetChild1().GetRoomCenter(), node.GetChild2().GetRoomCenter());
    }

    private void ConnectRooms(Vector2Int room1Center, Vector2Int room2Center)
    {
        if (_random.NextDouble() > 0.5)
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
    private System.Random _random;

    public TestBSP(VTools.Grid.Grid grid, RectInt bounds, int maxIterations, int minRoomSize, System.Random random, int currentIteration = 0)
    {
        _random = random;
        _grid = grid;
        _bounds = bounds;
        _maxIterations = maxIterations;
        _minRoomSize = minRoomSize;
        _currentIteration = currentIteration;
    }

    public void CreateRoom(int padding)
    {
        int roomWidth = Mathf.Max(_minRoomSize, _bounds.width - padding * 2);
        int roomHeight = Mathf.Max(_minRoomSize, _bounds.height - padding * 2);

        int roomX = _bounds.xMin + (_bounds.width - roomWidth) / 2;
        int roomY = _bounds.yMin + (_bounds.height - roomHeight) / 2;

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
        bool splitHorizontally;

        if (_bounds.width > _bounds.height)
        {
            splitHorizontally = false;
        }
        else if (_bounds.height > _bounds.width)
        {
            splitHorizontally = true;
        }
        else
        {
            splitHorizontally = _random.NextDouble() > 0.5;
        }

        if (splitHorizontally)
        {
            int splitY = _random.Next(_bounds.yMin + _minRoomSize, _bounds.yMax - _minRoomSize);

            _child1 = new TestBSP(
                _grid,
                new RectInt(_bounds.x, _bounds.y, _bounds.width, splitY - _bounds.y),
                _maxIterations,
                _minRoomSize,
                _random,
                _currentIteration + 1
            );

            _child2 = new TestBSP(
                _grid,
                new RectInt(_bounds.x, splitY, _bounds.width, _bounds.yMax - splitY),
                _maxIterations,
                _minRoomSize,
                _random,
                _currentIteration + 1
            );
        }
        else
        {
            int splitX = _random.Next(_bounds.xMin + _minRoomSize, _bounds.xMax - _minRoomSize);

            _child1 = new TestBSP(
                _grid,
                new RectInt(_bounds.x, _bounds.y, splitX - _bounds.x, _bounds.height),
                _maxIterations,
                _minRoomSize,
                _random,
                _currentIteration + 1
            );

            _child2 = new TestBSP(
                _grid,
                new RectInt(splitX, _bounds.y, _bounds.xMax - splitX, _bounds.height),
                _maxIterations,
                _minRoomSize,
                _random,
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
    public Vector2Int GetCenter() { return new Vector2Int((_bounds.xMin + _bounds.width) / 2, (_bounds.yMin + _bounds.height) / 2); }
    public Vector2Int GetRoomCenter()
    {
        if (IsLeaf())
        {
            return new Vector2Int(_room.xMin + _room.width / 2, _room.yMin + _room.height / 2);
        }

        if (_child1 != null)
            return _child1.GetRoomCenter();
        if (_child2 != null)
            return _child2.GetRoomCenter();

        return GetCenter();
    }
}