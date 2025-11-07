using Cysharp.Threading.Tasks;
using Microsoft.Unity.VisualStudio.Editor;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using VTools.Grid;
using VTools.RandomService;
using VTools.ScriptableObjectDatabase;
using VTools.Utility;

namespace Components.ProceduralGeneration.SimpleRoomPlacement
{
    [CreateAssetMenu(menuName = "Procedural Generation Method/Simple Room Placement")]

    public class SimpleRoomPlacement : ProceduralGenerationMethod 
        {
        [Header("Room Parameters")]
        [SerializeField] private int _maxRooms = 10;
        [SerializeField] private Vector2Int _roomMinSize = new Vector2Int(5,5);
        [SerializeField] private Vector2Int _roomMaxSize = new Vector2Int(12,8);

        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            List<RectInt> placedRooms = new();
            int roomsPlacedCount = 0;
            int attempts = 0;


            for (int i = 0; i < _maxSteps; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (roomsPlacedCount >= _maxRooms)
                {
                    break;
                }

                attempts++;

                int width = RandomService.Range(_roomMinSize.x, _roomMaxSize.x + 1);
                int length = RandomService.Range(_roomMinSize.y, _roomMaxSize.y + 1);

                int x = RandomService.Range(0, Grid.Width - width);
                int z = RandomService.Range(0, Grid.Lenght - length);

                bool overlaps = false;
                RectInt room = new RectInt(x, z, width, length);

                foreach (var existingRoom in placedRooms)
                {
                    if (room.Overlaps(existingRoom))
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                {
                    PlaceRoom(room);
                    placedRooms.Add(room);  
                    roomsPlacedCount++;
                    Debug.Log(roomsPlacedCount);
                }


                await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
            }

            for (int i = 0; i < placedRooms.Count - 1; i++)
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();

                Vector2Int start = placedRooms[i].GetCenter();
                Vector2Int end = placedRooms[i + 1].GetCenter();

                CreateDogLegCorridor(start, end);

                await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
            }

            BuildGround();
        }

        private void PlaceRoom(RectInt room)
        {
            for (int ix = room.xMin; ix < room.xMax; ix++)
            {
                for (int iy = room.yMin; iy < room.yMax; iy++)
                {
                    if (!Grid.TryGetCellByCoordinates(ix, iy, out var cell))
                        continue;

                    AddTileToCell(cell, ROOM_TILE_NAME, true);
                }
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

        private void BuildGround()
        {
            var groundTemplate = ScriptableObjectDatabase.GetScriptableObject<GridObjectTemplate>("Grass");
            
            // Instantiate ground blocks
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


    }
}