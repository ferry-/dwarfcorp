﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {
        public static bool VoxelIsCompletelySurrounded(VoxelHandle V)
        {
            if (V.Chunk == null)
                return false;

            foreach (var neighborCoordinate in VoxelHelpers.EnumerateManhattanNeighbors(V.Coordinate))
            {
                var voxelHandle = new VoxelHandle(V.Chunk.Manager.ChunkData, neighborCoordinate);
                if (!voxelHandle.IsValid) return false;
                if (voxelHandle.IsEmpty && voxelHandle.WaterCell.WaterLevel < 4) return false;
            }

            return true;
        }
    }
}
