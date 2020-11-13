﻿using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Tilemaps;


public class TestGeneration : MonoBehaviour
{
    [SerializeField] Tilemap tilemap, damageOverlayTilemap, oreTilemap;
    [SerializeField] TileBase[] groundTiles;
    [SerializeField] TileBase[] damageOverlayTiles;
    [SerializeField] TileBase[] oreTiles;
    [SerializeField] TileBase snowTile1, snowTile2;

    [Header("Settings")]
    [SerializeField] bool updateOnParameterChanged;

    [OnValueChanged("OnParameterChanged")]
    [SerializeField] bool seedIsRandom;

    [OnValueChanged("OnParameterChanged")]
    [SerializeField] int seed;

    [OnValueChanged("OnParameterChanged")]
    [SerializeField] int size;

    [OnValueChanged("OnParameterChanged")]
    [Range(0, 1)]
    [SerializeField] float initialAliveChance;

    [OnValueChanged("OnParameterChanged")]
    [Range(0, 9)]
    [SerializeField] int deathLimit;

    [OnValueChanged("OnParameterChanged")]
    [Range(0, 9)]
    [SerializeField] int birthLimit;

    [OnValueChanged("OnParameterChanged")]
    [Range(0, 10)]
    [SerializeField] int automataSteps;

    [SerializeField] AnimationCurve heightMultiplyer;
    [SerializeField] int snowStartHeight;

    [SerializeField] OrePass[] orePasses;

    [SerializeField] GameObject physicalTilePrefab;

    [Header("Debug")]
    [SerializeField] bool drawStabilityTexture;
    [SerializeField] bool drawStabilityGizmos;
    [SerializeField] int stabilityGizmosSize;
    [SerializeField] PlayerController player;

    Tile[,] map;
    Texture2D stabilityDebugTexture;

    private void Start()
    {
        RunCompleteGeneration();
    }


    [Button]
    private void RunCompleteGeneration()
    {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        Populate();

        Util.IterateX(automataSteps, (x) => RunAutomataStep());

        PopulateOres();

        CalculateNeighboursBitmask();

        CalculateStability();

        PopulateSnow();

        UpdateVisuals();

        stopwatch.Stop();

        Debug.Log("Update Duration: " + stopwatch.ElapsedMilliseconds + "ms");
    }

    private void CalculateStability()
    {
        Util.IterateXY(size, (x, y) => CalcuateStabilityAt(x, y));

    }

    private void CalcuateStabilityAt(int x, int y)
    {
        if (IsAirAt(x, y))
            return;

        int[] stabilityEffect = { 0, 5, 0, 10, 10, 0, 20, 0 };
        var neighbours = GetNeighboursIndiciesOf(x, y);

        int stability = 0;
        for (int i = 0; i < neighbours.Length; i++)
        {
            var neighbour = neighbours[i];
            stability += IsBlockAt(neighbour.x, neighbour.y) ? stabilityEffect[i] : 0;
        }


        var tile = GetTileAt(x, y);
        tile.Stability = stability;
        SetMapAt(x, y, tile, updateProperties: false, updateVisuals: false);
    }

    private void PopulateSnow()
    {
        Util.IterateXY(size, PopulateSnowAt);
    }

    private void PopulateSnowAt(int x, int y)
    {
        if (y < snowStartHeight)
            return;

        var t = GetTileAt(x, y);


        if (IsBlockAt(x, y) && ((t.NeighbourBitmask & 2) == 0))
        {
            t.Type = TileType.Snow;
        }

        SetMapAt(x, y, t, updateProperties: false, updateVisuals: false);
    }

    private void CalculateNeighboursBitmask()
    {
        Util.IterateXY(size, CalculateNeighboursBitmaskAt);
    }

    private void CalculateNeighboursBitmaskAt(int x, int y)
    {
        if (IsOutOfBounds(x, y))
            return;

        int topLeft = IsBlockAt(x - 1, y + 1) ? 1 : 0;
        int topMid = IsBlockAt(x, y + 1) ? 1 : 0;
        int topRight = IsBlockAt(x + 1, y + 1) ? 1 : 0;
        int midLeft = IsBlockAt(x - 1, y) ? 1 : 0;
        int midRight = IsBlockAt(x + 1, y) ? 1 : 0;
        int botLeft = IsBlockAt(x - 1, y - 1) ? 1 : 0;
        int botMid = IsBlockAt(x, y - 1) ? 1 : 0;
        int botRight = IsBlockAt(x + 1, y - 1) ? 1 : 0;

        int value = topMid * 2 + midLeft * 8 + midRight * 16 + botMid * 64;
        value += topLeft * topMid * midLeft;
        value += topRight * topMid * midRight * 4;
        value += botLeft * midLeft * botMid * 32;
        value += botRight * midRight * botMid * 128;

        Tile t = map[x, y];
        t.NeighbourBitmask = (byte)value;
        map[x, y] = t;

    }

    private void Populate()
    {
        if (!seedIsRandom)
            UnityEngine.Random.InitState(seed);

        map = new Tile[size, size];

        Util.IterateXY(size, PopulateAt);

    }

    private void PopulateOres()
    {
        foreach (var pass in orePasses)
        {
            Util.IterateX((int)(size * size * pass.Probability * 0.01f), (x) => TryPlaceVein(pass.TileType, Util.RandomInVector(pass.OreVeinSize), pass.MaxHeight));
        }
    }

    private void TryPlaceVein(TileType type, int amount, int maxHeight)
    {
        int y = UnityEngine.Random.Range(0, maxHeight);
        int x = UnityEngine.Random.Range(0, size);

        GrowVeinAt(x, y, type, amount);
    }

    private void GrowVeinAt(int startX, int startY, TileType tile, int amount)
    {
        int x = startX;
        int y = startY;
        int attemptsLeft = amount * 10;

        while (amount > 0 && attemptsLeft > 0)
        {
            if (IsBlockAt(x, y))
            {
                if (GetTileAt(x, y).Type != tile)
                {
                    SetMapAt(x, y, Tile.Make(tile), updateProperties: false, updateVisuals: false);
                    amount--;
                    x = startX;
                    y = startY;
                }
                else
                {
                    var dir = Util.RandomDirection();
                    x += dir.x;
                    y += dir.y;
                }
            }
            else
            {
                x = startX;
                y = startY;
            }
            attemptsLeft--;
        }
    }


    private void PopulateAt(int x, int y)
    {
        Tile t = Tile.Air;

        bool occupied = heightMultiplyer.Evaluate((float)y / size) * UnityEngine.Random.value < initialAliveChance;

        if (occupied)
            t.Type = TileType.Stone;

        map[x, y] = t;
    }

    //https://gamedevelopment.tutsplus.com/tutorials/generate-random-cave-levels-using-cellular-automata--gamedev-9664
    private int GetAliveNeightboursCountFor(int x, int y)
    {
        int count = 0;
        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                int nx = x + i;
                int ny = y + j;

                if (i == 0 && j == 0)
                {
                }
                else if (nx < 0 || ny < 0 || nx >= size || ny >= size)
                {
                    count = count + 1;
                }
                else if (IsBlockAt(nx, ny))
                {
                    count = count + 1;
                }
            }
        }

        return count;
    }

    public bool IsAirAt(int x, int y)
    {
        return GetTileAt(x, y).Type == TileType.Air;
    }

    public bool IsBlockAt(int x, int y)
    {
        return GetTileAt(x, y).Type != TileType.Air;
    }

    public Tile GetTileAt(int x, int y)
    {
        if (IsOutOfBounds(x, y))
            return Tile.Air;

        return map[x, y];
    }

    public bool HasLineOfSight(Vector2Int start, Vector2Int end, bool debugVisualize = false)
    {
        Vector2Int current = start;

        while (current != end)
        {
            bool blocked = IsBlockAt(current.x, current.y);

            if (blocked)
            {
                if (debugVisualize)
                    Debug.DrawLine((Vector3Int)current, (Vector3Int)end, Color.red, 1);
                return false;
            }

            Vector2Int offset = StepTowards(current, end);
            if (debugVisualize)
                Debug.DrawLine((Vector3Int)current, (Vector3Int)(current + offset), Color.yellow, 1f);
            current += offset;
        }

        return true;
    }

    public static Vector2Int StepTowards(Vector2Int current, Vector2Int end)
    {
        Vector2Int delta = end - current;
        Vector2Int offset;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            offset = new Vector2Int((int)Mathf.Sign(delta.x), 0);
        else if (Mathf.Abs(delta.x) < Mathf.Abs(delta.y))
            offset = new Vector2Int(0, (int)Mathf.Sign(delta.y));
        else
            offset = new Vector2Int((int)Mathf.Sign(delta.x), (int)Mathf.Sign(delta.y));

        return offset;
    }

    public static Vector3 StepTowards(Vector3 current, Vector3 end)
    {
        Vector3 delta = end - current;
        Vector3 offset;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            offset = new Vector3((int)Mathf.Sign(delta.x), 0);
        else if (Mathf.Abs(delta.x) < Mathf.Abs(delta.y))
            offset = new Vector3(0, (int)Mathf.Sign(delta.y));
        else
            offset = new Vector3((int)Mathf.Sign(delta.x), (int)Mathf.Sign(delta.y));

        return offset;
    }

    public Vector3 GetWorldLocationOfFreeFaceFromSource(Vector2Int target, Vector2Int source)
    {
        Vector2Int disp = source - target;

        if (Mathf.Abs(disp.x) > Mathf.Abs(disp.y))
        {
            bool xAir = IsAirAt(target.x + (int)Mathf.Sign(disp.x), target.y);
            if (xAir)
                return (Vector3Int)target + new Vector3((int)Mathf.Sign(disp.x) * 0.5f + 0.5f, 0.5f, 0);
            else
                return (Vector3Int)target + new Vector3(0.5f, (int)Mathf.Sign(disp.y) * 0.5f + 0.5f, 0);
        }
        else
        {
            bool yAir = IsAirAt(target.x, target.y + (int)Mathf.Sign(disp.y));
            if (yAir)
                return (Vector3Int)target + new Vector3(0.5f, (int)Mathf.Sign(disp.y) * 0.5f + 0.5f, 0);
            else
                return (Vector3Int)target + new Vector3((int)Mathf.Sign(disp.x) * 0.50f + 0.5f, 0.5f, 0);
        }

    }

    public Vector2Int GetClosestSolidBlock(Vector2Int current, Vector2Int end)
    {
        while (current != end)
        {
            if (IsBlockAt(current.x, current.y))
                return current;

            current += StepTowards(current, end);
        }
        return end;
    }

    public bool DamageAt(int x, int y, float amount)
    {
        if (IsOutOfBounds(x, y))
            return false;

        Tile t = GetTileAt(x, y);
        t.TakeDamage(amount);

        //
        if (t.Damage > 10)
        {
            BreakBlock(x, y, t);

            return true;
        }
        else
        {
            SetMapAt(x, y, t, updateProperties: false, updateVisuals: true);
            return false;
        }
    }

    private void BreakBlock(int x, int y, Tile t)
    {
        CarveAt(x, y);

        ItemType itemType = ItemType.ROCKS;

        switch (t.Type)
        {
            case TileType.Gold:
                itemType = ItemType.GOLD;
                break;

            case TileType.Copper:
                itemType = ItemType.COPPER;
                break;
        }

        InventoryManager.PlayerCollects(itemType, 1);
    }

    public void CarveAt(int x, int y)
    {
        //Debug.Log("Try Carve " + x + " / " + y);
        SetMapAt(x, y, Tile.Air);
    }

    public void PlaceAt(int x, int y)
    {
        Debug.Log("Try Place " + x + " / " + y);
        SetMapAt(x, y, Tile.Make(TileType.Stone));
    }

    private void SetMapAt(int x, int y, Tile value, bool updateProperties = true, bool updateVisuals = true)
    {
        if (IsOutOfBounds(x, y))
            return;

        map[x, y] = value;

        if (updateProperties)
        {
            CalcuateStabilityAt(x, y);
            CalculateNeighboursBitmaskAt(x, y);


            foreach (var nIndex in GetNeighboursIndiciesOf(x, y))
            {
                CalcuateStabilityAt(nIndex.x, nIndex.y);

                if(ShouldCollapseAt(nIndex.x, nIndex.y))
                {
                    CollapseAt(nIndex.x, nIndex.y);
                }

                CalculateNeighboursBitmaskAt(nIndex.x, nIndex.y);
            }
        }

        if (updateVisuals)
        {
            UpdateVisualsAt(x, y);
            foreach (var nIndex in GetNeighboursIndiciesOf(x, y))
            {
                UpdateVisualsAt(nIndex.x, nIndex.y);
            }
        }
    }

    private bool ShouldCollapseAt(int x, int y)
    {
        return IsBlockAt(x,y) && GetTileAt(x, y).Stability <= 10;
    }

    private void CollapseAt(int x, int y)
    {
        SetMapAt(x, y, Tile.Air, updateProperties: true, updateVisuals: false);
        var go = Instantiate(physicalTilePrefab, new Vector3(x + 0.5f, y + 0.5f, 0), Quaternion.identity);
        go.GetComponent<PhysicalTile>().Setup(this);
    }

    private Vector2Int[] GetNeighboursIndiciesOf(int x, int y)
    {
        return new Vector2Int[]
        {
            new Vector2Int(x-1,y+1),
            new Vector2Int(x,y+1),
            new Vector2Int(x+1,y+1),
            new Vector2Int(x-1,y),
            new Vector2Int(x+1,y),
            new Vector2Int(x-1,y-1),
            new Vector2Int(x,y-1),
            new Vector2Int(x+1,y-1)
        };
    }

    private void RunAutomataStep()
    {
        Util.IterateXY(size, SingleAutomataSet);
    }

    private void SingleAutomataSet(int x, int y)
    {
        int nbs = GetAliveNeightboursCountFor(x, y);
        map[x, y] = IsBlockAt(x, y) ? (nbs > deathLimit ? Tile.Make(TileType.Stone) : Tile.Air) : (nbs > birthLimit ? Tile.Make(TileType.Stone) : Tile.Air);
    }

    void UpdateVisuals()
    {
        tilemap.ClearAllTiles();
        damageOverlayTilemap.ClearAllTiles();
        oreTilemap.ClearAllTiles();
        Util.IterateXY(size, UpdateVisualsAt);
    }

    void OnParameterChanged()
    {
        if (updateOnParameterChanged)
        {
            RunCompleteGeneration();
        }
    }

    private void UpdateVisualsAt(int x, int y)
    {
        tilemap.SetTile(new Vector3Int(x, y, 0), GetVisualTileFor(x, y));
        damageOverlayTilemap.SetTile(new Vector3Int(x, y, 0), GetVisualDestructableOverlayFor(x, y));
        oreTilemap.SetTile(new Vector3Int(x, y, 0), GetVisualOreTileFor(x, y));

    }

    private bool IsOutOfBounds(int x, int y)
    {
        return (x < 0 || y < 0 || x >= size || y >= size);
    }

    private TileBase GetVisualTileFor(int x, int y)
    {
        Tile tile = GetTileAt(x, y);

        if (IsOutOfBounds(x, y) || IsAirAt(x, y))
            return null;

        if (tile.Type == TileType.Snow)
        {
            return Util.PseudoRandomValue(x, y) > 0.5f ? snowTile1 : snowTile2;
        }

        int tileIndex = Util.BITMASK_TO_TILEINDEX[tile.NeighbourBitmask];

        //Casual random tile
        if (tileIndex == 46)
        {
            tileIndex = Util.PseudoRandomValue(x, y) > 0.5f ? 46 : 0;
        }

        return groundTiles[tileIndex];
    }

    private TileBase GetVisualDestructableOverlayFor(int x, int y)
    {
        var t = GetTileAt(x, y);
        return damageOverlayTiles[Mathf.FloorToInt(t.Damage)];
    }

    private TileBase GetVisualOreTileFor(int x, int y)
    {
        var t = GetTileAt(x, y);
        if ((int)t.Type < 2 || (int)t.Type == 4)
            return null;

        return oreTiles[(int)t.Type - 2];
    }

    private void OnGUI()
    {
        if (drawStabilityTexture)
        {
            if (stabilityDebugTexture == null)
                UpdateDebugTextures();

            GUI.DrawTexture(new Rect(10, 10, size * 4, size * 4), stabilityDebugTexture);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawStabilityGizmos || map == null)
            return;


        for (int y = -stabilityGizmosSize; y < stabilityGizmosSize; y++)
        {
            for (int x = -stabilityGizmosSize; x < stabilityGizmosSize; x++)
            {
                Vector2Int pos = player.GetPositionInGrid() + new Vector2Int(x, y);
                Gizmos.color = StabilityToColor(GetTileAt(pos.x, pos.y).Stability);
                Gizmos.DrawCube((Vector3Int)pos + new Vector3(0.5f, 0.5f), new Vector3(1, 1, 0));
            }
        }

    }

    [Button(null, EButtonEnableMode.Playmode)]
    private void UpdateDebugTextures()
    {
        stabilityDebugTexture = new Texture2D(size, size);
        stabilityDebugTexture.filterMode = FilterMode.Point;

        Util.IterateXY(size, (x, y) => stabilityDebugTexture.SetPixel(x, y, StabilityToColor(GetTileAt(x, y).Stability)));
        stabilityDebugTexture.Apply();
    }

    private Color StabilityToColor(float stability)
    {


        if (stability > 20)
            return Color.white;
        else if (stability > 10)
            return Color.grey;
        else if (stability >= 0)
            return Color.red;
        else
            return Color.black;
    }
}
