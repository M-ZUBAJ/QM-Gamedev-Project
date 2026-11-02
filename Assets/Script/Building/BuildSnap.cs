/*
First Date: 31/10/2026
Date: 2/11/2026
Author: Tony
Use by: Nothing to set up (code only, no component). BlueprintPlacer uses it in Snap mode.
Unity Editor: Nothing to set. Each Building Data's Piece Type (Floor / Wall / Roof / Other) decides what it snaps to.
              Testing: Play, press B, build a floor, then choose a wall: aim near the floor's edge and the
              blueprint locks onto it (green). Scroll the mouse wheel: it turns 90 degrees and fits again
              (e.g. across the floor, or at another wall's end as an L corner).
Purpose: Snap building (like Valheim / Rust): finds the spot where a new piece fits against the built pieces where
         the player aims, and locks the blueprint there. No snap points have to be placed on the prefabs: every
         spot is worked out from the pieces' boxes (their sizes), so any new piece snaps as soon as it has a
         Piece Type. Different types snap to each other:
           Floor -> next to a floor; over a floor that has walls, resting on the walls (a ceiling / second floor).
           Wall  -> on a floor (along an edge, or across the middle); next to a floor; on top of a wall;
                    next to a wall: in line (a longer wall), at its end (L corner) or on its side (T).
           Roof  -> over a floor that has walls, resting on the walls; next to a roof.
           Other -> furniture (crafting table, furnace...): on a floor or on other furniture; against a wall;
                    next to a floor or other furniture.
         Next to = touching it with the same bottom height, lined up with one end, the middle or the other end.
         On top = flush with a corner or an edge, or in the middle (something bigger than what it sits on, like a
         wide roof, is always centered).
         Turning: until the player turns the blueprint (mouse wheel), every direction is tried and the best fit
         wins (a wall follows the floor edge by itself; ties go to the way the player faces). After that, only the
         player's direction is used: each wheel step turns it 90 degrees and it fits again in its new direction.
         A spot is skipped when the same piece is already built there, or when it would go into another built
         piece (walls may cross walls, so two walls can meet at a corner).
Requirement: BuildSnap.cs x-> BuiltPiece.cs, BuildingData.cs
             BuildSnap.cs <-x BlueprintPlacer.cs
*/

using System.Collections.Generic;
using UnityEngine;

public static class BuildSnap
{
    // One place the new piece could lock to.
    struct Spot
    {
        public Vector3 center;     // world position of the new piece's box middle
        public Quaternion turn;    // which way it faces
        public float score;        // how far it is from where the player aims (smaller = better)
    }

    // Sorts spots best first. A class, not a lambda: List.Sort with a lambda makes garbage every call.
    class ByScore : IComparer<Spot>
    {
        public int Compare(Spot a, Spot b)
        {
            return a.score.CompareTo(b.score);
        }
    }

    // Where the new piece lines up along one side of the piece it snaps to.
    enum Line { Start, Middle, End }

    // ===== Fields =====
    const float Touch = 0.05f;           // boxes pushing into each other less than this (meters) are only touching
    const float SameSpot = 0.1f;         // the same piece closer than this to a spot = it's already built there
    const float OnTopTolerance = 0.1f;   // a wall's bottom this close to a height counts as standing on it
    const int MaxWallRows = 8;           // walls stacked on walls: how many rows up the wall top is followed
    const float CenterWeight = 0.1f;     // score: "its middle is near the aim" only picks between spots at the aim
    const float AcrossPenalty = 0.02f;   // auto turning: a quarter turn from the piece counts as this much further
    const float TurnPenalty = 0.01f;     // auto turning: each quarter turn away from the way the player faces

    static readonly Line[] Lines = { Line.Start, Line.Middle, Line.End };
    static readonly List<Spot> spots = new List<Spot>();   // reused, so searching makes no garbage
    static readonly ByScore byScore = new ByScore();

    // The search going on right now (set by Find, so the helpers below don't need long parameter lists).
    static PieceType newType;    // the piece being placed
    static Bounds newBox;        // its box, in its own frame
    static Ray aim;              // the crosshair line
    static float aimRange;       // how far along it the player can build
    static bool aimHits;         // the crosshair points at a surface within range
    static RaycastHit aimHit;    // that surface: where the crosshair touches it, and which way it faces
    static float maxDistance;    // spots further than this from the aim don't count

    // ===== My methods =====

    // Finds the best spot for a piece of this type: the one closest to where the player aims (the surface the
    // crosshair points at, or the crosshair line when it points at the sky), if it's within "distance" meters.
    // box = the new piece's box in its own frame (BuiltPiece.MeasureLocalBox). hasHit / hit = what the crosshair
    // points at (Physics.Raycast). yaw = the way the player turned it. turnedByPlayer: false = every direction is
    // tried (best fit wins), true = only yaw. Returns false if nothing is close enough.
    public static bool Find(PieceType type, Bounds box, Ray ray, float range, bool hasHit, RaycastHit hit,
                            float distance, float yaw, bool turnedByPlayer,
                            out Vector3 position, out Quaternion turn)
    {
        position = Vector3.zero;
        turn = Quaternion.identity;
        if (type == PieceType.Auto) return false;

        newType = type;
        newBox = box;
        aim = ray;
        aimRange = range;
        aimHits = hasHit;
        aimHit = hit;
        maxDistance = distance;

        spots.Clear();
        foreach (BuiltPiece piece in BuiltPiece.All)
        {
            // Only pieces near the aim are worth checking: a spot touches its piece and must be near the aim.
            float reach = distance + piece.LocalBox.extents.magnitude + 2f * box.extents.magnitude;
            if (DistanceToAim(piece.Center) > reach) continue;

            int facing = QuarterTurns(yaw - piece.Yaw);   // the player's direction, in quarter turns from this piece
            for (int k = 0; k < 4; k++)
            {
                if (turnedByPlayer && k != facing) continue;

                int first = spots.Count;
                AddSpots(piece, k);
                if (turnedByPlayer) continue;

                // Auto turning: turned like the piece (or back to front) is best, then the closest to the player's way.
                float off = Mathf.Abs(Mathf.DeltaAngle(piece.Yaw + k * 90f, yaw)) / 90f;   // 0 = the player's way, 2 = opposite
                float extra = (k % 2 == 1 ? AcrossPenalty : 0f) + TurnPenalty * off;
                for (int i = first; i < spots.Count; i++)
                {
                    Spot s = spots[i];
                    s.score += extra;
                    spots[i] = s;
                }
            }
        }

        spots.Sort(byScore);   // best first
        foreach (Spot s in spots)
        {
            if (Blocked(s)) continue;   // already built there, or it would go into another built piece

            turn = s.turn;
            position = s.center - s.turn * box.center;   // from "where the box's middle goes" to the pivot
            return true;
        }
        return false;
    }

    // ----- The rules: which spots each type gets -----

    // Every spot a new piece, turned k quarter turns from this built piece, can take against it.
    static void AddSpots(BuiltPiece piece, int k)
    {
        Vector3 s = Size(newBox, k);   // the new piece's size, measured along this piece's sides
        Bounds p = piece.LocalBox;

        switch (newType)
        {
            case PieceType.Floor:
                if (piece.Type == PieceType.Floor)
                {
                    AddBeside(piece, s, k);      // floor next to floor
                    AddOverWalls(piece, s, k);   // a ceiling / second floor resting on its walls
                }
                break;

            case PieceType.Wall:
                if (piece.Type == PieceType.Floor)
                {
                    AddOnTop(piece, s, k, p.max.y);   // along an edge, or across the middle
                    AddBeside(piece, s, k);           // standing next to the floor
                }
                else if (piece.Type == PieceType.Wall)
                {
                    if (LongAlongZ(s) == LongAlongZ(p.size))
                    {
                        // In line: on top (a taller wall) and at its ends (a longer wall). Not against its long
                        // sides (a double-thick wall), and never lying across its top.
                        AddOnTop(piece, s, k, p.max.y);
                        bool longZ = LongAlongZ(p.size);
                        AddBeside(piece, s, k, !longZ, longZ);
                    }
                    else
                    {
                        AddBeside(piece, s, k);   // turned: at its end = an L corner, on its side = a T
                    }
                }
                break;

            case PieceType.Roof:
                if (piece.Type == PieceType.Floor) AddOverWalls(piece, s, k);
                else if (piece.Type == PieceType.Roof) AddBeside(piece, s, k);
                break;

            case PieceType.Other:
                if (piece.Type == PieceType.Floor || piece.Type == PieceType.Other) AddOnTop(piece, s, k, p.max.y);
                if (piece.Type != PieceType.Roof) AddBeside(piece, s, k);   // against a wall, next to a floor or furniture
                break;
        }
    }

    // Next to the piece, touching it, its bottom at the same height as the piece's bottom, lined up with the start,
    // the middle or the end of that side. leftRight / frontBack = which of the piece's sides to use (default: all 4).
    static void AddBeside(BuiltPiece piece, Vector3 s, int k, bool leftRight = true, bool frontBack = true)
    {
        Bounds p = piece.LocalBox;
        float y = p.min.y + s.y / 2f;

        foreach (Line line in Lines)
        {
            if (leftRight && BesideLine(line, s.z, p.size.z))
            {
                float z = LineUp(p.min.z, p.max.z, s.z, line);
                Add(piece, new Vector3(p.max.x + s.x / 2f, y, z), k);   // right side
                Add(piece, new Vector3(p.min.x - s.x / 2f, y, z), k);   // left side
            }
            if (frontBack && BesideLine(line, s.x, p.size.x))
            {
                float x = LineUp(p.min.x, p.max.x, s.x, line);
                Add(piece, new Vector3(x, y, p.max.z + s.z / 2f), k);   // front
                Add(piece, new Vector3(x, y, p.min.z - s.z / 2f), k);   // back
            }
        }
    }

    // Standing at a height of the piece's frame (its top, or the top of its walls), over the piece's area:
    // flush with a corner, flush with an edge, or in the middle (up to 9 spots).
    static void AddOnTop(BuiltPiece piece, Vector3 s, int k, float height)
    {
        Bounds p = piece.LocalBox;
        float y = height + s.y / 2f;

        foreach (Line lineX in Lines)
        {
            if (!OnTopLine(lineX, s.x, p.size.x)) continue;
            foreach (Line lineZ in Lines)
            {
                if (!OnTopLine(lineZ, s.z, p.size.z)) continue;
                Add(piece, new Vector3(LineUp(p.min.x, p.max.x, s.x, lineX), y, LineUp(p.min.z, p.max.z, s.z, lineZ)), k);
            }
        }
    }

    // Next to a piece, lining up with a side's start or end only makes a new spot when the sizes differ
    // (shorter = flush with one end, longer = sticks out past one end, like the arm of an L corner).
    static bool BesideLine(Line line, float size, float side)
    {
        return line == Line.Middle || Mathf.Abs(size - side) >= Touch;
    }

    // On top of a piece, only something smaller than that side lines up with its edges. As big or bigger, it's
    // centered (e.g. a wide roof over a floor overhangs both sides the same, not just one).
    static bool OnTopLine(Line line, float size, float side)
    {
        return line == Line.Middle || size < side - Touch;
    }

    // Over a floor that has walls standing on it, resting on top of the walls (a roof, a ceiling / second floor),
    // lined up with the floor. No walls on the floor = no spot (something has to hold it up).
    static void AddOverWalls(BuiltPiece floor, Vector3 s, int k)
    {
        if (!WallTop(floor, out float top)) return;
        AddOnTop(floor, s, k, top - floor.Position.y);   // the floor only turns around "up", so heights just subtract
    }

    // The top of the walls standing on this floor (walls stacked on them count too), as a world height.
    // False if no wall stands on it.
    static bool WallTop(BuiltPiece floor, out float top)
    {
        top = floor.Position.y + floor.LocalBox.max.y;   // start at the floor's own top
        bool found = false;

        for (int row = 0; row < MaxWallRows; row++)   // one row of walls at a time, going up
        {
            float rowTop = top;
            foreach (BuiltPiece wall in BuiltPiece.All)
            {
                if (wall.Type != PieceType.Wall || !Over(wall, floor)) continue;

                float bottom = wall.Position.y + wall.LocalBox.min.y;
                if (Mathf.Abs(bottom - top) > OnTopTolerance) continue;   // not standing at this height
                rowTop = Mathf.Max(rowTop, wall.Position.y + wall.LocalBox.max.y);
            }

            if (rowTop <= top) break;   // nothing stands at this height: the top is reached
            top = rowTop;
            found = true;
        }
        return found;
    }

    // True if the piece's middle is over the floor's area (seen from above).
    static bool Over(BuiltPiece piece, BuiltPiece floor)
    {
        Vector3 local = floor.ToLocal(piece.Center);
        Bounds p = floor.LocalBox;

        return local.x >= p.min.x - OnTopTolerance && local.x <= p.max.x + OnTopTolerance
            && local.z >= p.min.z - OnTopTolerance && local.z <= p.max.z + OnTopTolerance;
    }

    // ----- Spots -----

    // Adds a spot given in the built piece's own frame, turned k quarter turns from that piece,
    // if it's close enough to where the player aims.
    static void Add(BuiltPiece piece, Vector3 localCenter, int k)
    {
        Vector3 center = piece.ToWorld(localCenter);
        Quaternion turn = piece.Turn * Quaternion.Euler(0f, k * 90f, 0f);
        Vector3 half = newBox.extents;

        // The aim: the point the crosshair touches, or (at the sky) the point on its line closest to the spot.
        Vector3 point = aimHits ? aimHit.point : ClosestOnAim(center);
        float distance = DistanceToBox(point, center, turn, half);
        if (distance > maxDistance) return;

        // Between spots right at the aim, the one whose middle is closest to where Surface mode would put it wins
        // (sitting on the surface the crosshair touches, pushed out of it by its own size).
        Vector3 surfaceCenter = aimHits ? aimHit.point + aimHit.normal * HalfSizeAlong(aimHit.normal, turn, half) : point;
        float score = distance + CenterWeight * Vector3.Distance(center, surfaceCenter);

        spots.Add(new Spot { center = center, turn = turn, score = score });
    }

    // True if this spot can't be used: the same piece is already built there, or the new piece would go into
    // another built piece. Walls may go into walls, so two walls can meet at a floor's corner.
    static bool Blocked(Spot spot)
    {
        Vector3 half = newBox.extents;
        foreach (BuiltPiece piece in BuiltPiece.All)
        {
            Vector3 center = piece.Center;
            Vector3 pieceHalf = piece.LocalBox.extents;
            float reach = half.magnitude + pieceHalf.magnitude;
            if ((center - spot.center).sqrMagnitude > reach * reach) continue;   // too far apart to touch

            if (piece.Type == newType && Vector3.Distance(center, spot.center) < SameSpot
                && SameLine(piece.Turn, spot.turn)) return true;   // the same piece is already built there
            if (newType == PieceType.Wall && piece.Type == PieceType.Wall) continue;
            if (BoxesOverlap(spot.center, spot.turn, half, center, piece.Turn, pieceHalf)) return true;
        }
        return false;
    }

    // ----- Box math -----

    // True if two boxes (turned only around "up") push into each other by more than Touch.
    static bool BoxesOverlap(Vector3 centerA, Quaternion turnA, Vector3 halfA,
                             Vector3 centerB, Quaternion turnB, Vector3 halfB)
    {
        if (Mathf.Abs(centerA.y - centerB.y) >= halfA.y + halfB.y - Touch) return false;   // one is above the other

        // Seen from above they are two turned rectangles: they overlap unless a gap shows along one of their sides.
        Vector3 between = centerB - centerA;
        between.y = 0f;
        return !GapAlong(turnA * Vector3.right, between, turnA, halfA, turnB, halfB)
            && !GapAlong(turnA * Vector3.forward, between, turnA, halfA, turnB, halfB)
            && !GapAlong(turnB * Vector3.right, between, turnA, halfA, turnB, halfB)
            && !GapAlong(turnB * Vector3.forward, between, turnA, halfA, turnB, halfB);
    }

    // True if, measured along this flat direction, the two boxes don't reach each other.
    static bool GapAlong(Vector3 direction, Vector3 between,
                         Quaternion turnA, Vector3 halfA, Quaternion turnB, Vector3 halfB)
    {
        float reach = HalfSizeAlong(direction, turnA, halfA) + HalfSizeAlong(direction, turnB, halfB);
        return Mathf.Abs(Vector3.Dot(between, direction)) >= reach - Touch;
    }

    // How far a box (turned only around "up") reaches from its middle along a direction.
    static float HalfSizeAlong(Vector3 direction, Quaternion turn, Vector3 half)
    {
        return half.x * Mathf.Abs(Vector3.Dot(direction, turn * Vector3.right))
             + half.y * Mathf.Abs(direction.y)
             + half.z * Mathf.Abs(Vector3.Dot(direction, turn * Vector3.forward));
    }

    // How far a point is from a turned box (0 = inside it or on it).
    static float DistanceToBox(Vector3 point, Vector3 center, Quaternion turn, Vector3 half)
    {
        Vector3 local = Quaternion.Inverse(turn) * (point - center);
        Vector3 outside = new Vector3(Mathf.Max(Mathf.Abs(local.x) - half.x, 0f),
                                      Mathf.Max(Mathf.Abs(local.y) - half.y, 0f),
                                      Mathf.Max(Mathf.Abs(local.z) - half.z, 0f));
        return outside.magnitude;
    }

    // How far the aim is from a point: from the point the crosshair touches, or (at the sky) from its line.
    static float DistanceToAim(Vector3 point)
    {
        return Vector3.Distance(point, aimHits ? aimHit.point : ClosestOnAim(point));
    }

    // The point on the crosshair line (within aimRange) closest to this point.
    static Vector3 ClosestOnAim(Vector3 point)
    {
        float along = Mathf.Clamp(Vector3.Dot(point - aim.origin, aim.direction), 0f, aimRange);
        return aim.origin + aim.direction * along;
    }

    // Where a piece "size" long goes along one side of another piece (from lo to hi):
    // flush with the side's start, in its middle, or flush with its end.
    static float LineUp(float lo, float hi, float size, Line line)
    {
        switch (line)
        {
            case Line.Start: return lo + size / 2f;
            case Line.End: return hi - size / 2f;
            default: return (lo + hi) / 2f;
        }
    }

    // The new piece's size after turning it k quarter turns (a quarter turn swaps its width and depth).
    static Vector3 Size(Bounds box, int k)
    {
        Vector3 s = box.size;
        return k % 2 == 0 ? s : new Vector3(s.z, s.y, s.x);
    }

    // True if a size is longer along z than along x (which way a wall runs).
    static bool LongAlongZ(Vector3 size)
    {
        return size.z >= size.x;
    }

    // True if two turns face the same way or exactly opposite (a box covers the same ground both ways).
    static bool SameLine(Quaternion a, Quaternion b)
    {
        float angle = Mathf.Abs(Mathf.DeltaAngle(a.eulerAngles.y, b.eulerAngles.y));
        return angle < 1f || angle > 179f;
    }

    // An angle in degrees -> the nearest number of quarter turns, 0 - 3.
    static int QuarterTurns(float degrees)
    {
        return ((Mathf.RoundToInt(degrees / 90f) % 4) + 4) % 4;
    }
}
