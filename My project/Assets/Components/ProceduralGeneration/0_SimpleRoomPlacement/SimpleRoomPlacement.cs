using Cysharp.Threading.Tasks;
using Microsoft.Unity.VisualStudio.Editor;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using VTools.Grid;
using VTools.RandomService;
using VTools.ScriptableObjectDatabase;

namespace Components.ProceduralGeneration.SimpleRoomPlacement
{
    [CreateAssetMenu(menuName = "Procedural Generation Method/Simple Room Placement")]

    public class SimpleRoomPlacement : ProceduralGenerationMethod 
        {
        [Header("Room Parameters")]
        [SerializeField] private int _maxRooms = 10;

        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            List<RectInt> storage = new List<RectInt>();
            
            for (int i = 0; i < _maxSteps; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int x = RandomService.Range(0, Grid.Width);
                int z = RandomService.Range(0, Grid.Lenght);

                bool overlaps = false;
                RectInt room = new RectInt(x, z, 10, 10);

                foreach (var existingRoom in storage)
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
                    storage.Add(room);
                }


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

        private void PlaceLines(List<RectInt> storage)
        {

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