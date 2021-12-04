using Godot;
using System;
using System.Collections.Generic;

public class WorldGenerator
{

    //Cutoffs
    public float deepSeaCutoff = 0.48f;
    public float seaCutoff = 0.5f;
    public float beachCutoff = 0.515f;
    public float iceBeachCutoff = 0.508f;
    public float landCutoff = 0.63f;
    public float desertLandCutoff = 0.63f;
    public float iceLandCutoff = 0.63f;
    public float mountainCutoff = 0.67f;
    public float snowCutoff = 1.0f;
    public float forestCutoff = 0.62f;
    public float desertTempuratureCutoff = 0.11f;
    public float iceTempuratureCutoff = 0.75f;

    //Thresholds
    public float forestThreshold = 0.56f;
    public float iceBeachThreshold = 0.5f;
    public float desertLandThreshold = 0.535f;
    public float iceLandThreshold = 0.508f;

    //Influences / Limits
    public float noiseInfluenceDivisorOnTempurature = 4.0f;
    public int maxRiverCount = 30;
    public float percentChanceOfRiverInDesert = 30.0f;
    public float percentChanceOfRiverInSnow = 40.0f;

    //Colors
    public Color deepSeaColor = Colors.DarkBlue;
    public Color seaColor = Colors.Blue;
    public Color beachColor = Colors.SandyBrown;
    public Color iceBeachColor = Colors.Teal;
    public Color landColor = Colors.ForestGreen;
    public Color desertLandColor = Colors.DarkGoldenrod;
    public Color iceLandColor = Colors.LightGray;
    public Color mountainColor = Colors.DarkGray;
    public Color desertMountainColor = Colors.Goldenrod;
    public Color iceMountainColor = Colors.White;
    public Color snowColor = Colors.White;
    public Color forestColor = Colors.DarkGreen;
    public Color errorColor = Colors.HotPink;

    enum TileTypes
    {
        Empty,
        DeepSea,
        Sea,
        Beach,
        IceBeach,
        Land,
        DesertLand,
        IceLand,
        Mountain,
        DesertMountain,
        IceMountain,
        Snow,
        Forest
    }

    Dictionary<TileTypes, Color> tileColors;

    //Noise Settings
    public int octaves = 6;
    public float period = 150f;
    public float persistance = 0.55f;
    public float lacunarity = 1.8f;
    public int seed = 0;

    //Data variables
    Image noiseImage;
    int[,] tileGrid;
    Godot.OpenSimplexNoise noise;
    Godot.RandomNumberGenerator rng;

    private void InitVariables()
    {
        noise = new Godot.OpenSimplexNoise();
        rng = new Godot.RandomNumberGenerator();
        rng.Randomize();
        seed = (int)rng.Randi();

        SeedNoise(seed);
        UpdateVariables();

        noiseImage = CreateSeamlessRGBASimplexData();
    }

    public void Regenerate()
    {
        SeedNoise(seed);
        UpdateVariables();

        noiseImage = CreateSeamlessRGBASimplexData();
        GenerateTileGridFromSimplexNoise();
    }

    private void SeedNoise(int seed)
    {
        noise.Seed = seed;
    }

    public void SetSeed(ulong seed)
    {
        this.seed = (int)seed;
        rng.Seed = (ulong)this.seed;
    }

    public void RandomizeSeed()
    {
        int seed = (int)rng.Randi();
        this.seed = seed;
        rng.Seed = (ulong)this.seed;
    }

    private void UpdateVariables()
    {
        //Noise
        noise.Octaves = octaves;
        noise.Period = period;
        noise.Persistence = persistance;
        noise.Lacunarity = lacunarity;

        //Colors
        tileColors = new Dictionary<TileTypes, Color>();
        tileColors.Add(TileTypes.Beach, beachColor);
        tileColors.Add(TileTypes.DeepSea, deepSeaColor);
        tileColors.Add(TileTypes.DesertLand, desertLandColor);
        tileColors.Add(TileTypes.DesertMountain, desertMountainColor);
        tileColors.Add(TileTypes.Empty, errorColor);
        tileColors.Add(TileTypes.Forest, forestColor);
        tileColors.Add(TileTypes.IceBeach, iceBeachColor);
        tileColors.Add(TileTypes.IceLand, iceLandColor);
        tileColors.Add(TileTypes.IceMountain, iceMountainColor);
        tileColors.Add(TileTypes.Land, landColor);
        tileColors.Add(TileTypes.Mountain, mountainColor);
        tileColors.Add(TileTypes.Sea, seaColor);
        tileColors.Add(TileTypes.Snow, snowColor);
    }

    private Image CreateSeamlessRGBASimplexData()
    {
        Image result = noise.GetSeamlessImage(1280);
        result.Convert(Image.Format.Rgbaf);
        return result;
    }

    private void GenerateTileGridFromSimplexNoise()
    {
        if (noiseImage == null) noiseImage = CreateSeamlessRGBASimplexData();
        tileGrid = new int[noiseImage.GetWidth(), noiseImage.GetHeight()];
        tileGrid = CreateTileGrid(tileGrid);
    }

    private int[,] CreateTileGrid(int[,] grid)
    {
        noiseImage.Lock();
        List<Vector2> validRiverPoints = new List<Vector2>();
        for (int x = 0; x < noiseImage.GetWidth(); x++)
        {
            for (int y = 0; y < noiseImage.GetHeight(); y++)
            {
                //Base
                Color pix = noiseImage.GetPixel(x, y);
                if (pix.r < deepSeaCutoff) grid[x, y] = (int)TileTypes.DeepSea;
                else if (pix.r < seaCutoff) grid[x, y] = (int)TileTypes.Sea;
                else if (pix.r < beachCutoff) grid[x, y] = (int)TileTypes.Beach;
                else if (pix.r < landCutoff) grid[x, y] = (int)TileTypes.Land;
                else if (pix.r < mountainCutoff) grid[x, y] = (int)TileTypes.Mountain;
                else if (pix.r < snowCutoff) grid[x, y] = (int)TileTypes.Snow;
                else grid[x, y] = (int)TileTypes.Empty;

                //getValidRiverPoints
                if (pix.r > landCutoff) validRiverPoints.Add(new Vector2(x, y));

                //Forests
                if (pix.r < forestCutoff && pix.r > forestThreshold) grid[x, y] = (int)TileTypes.Forest;

                //CalcTempurature
                grid[x, y] = (int)CalcTempurature((TileTypes)grid[x, y], pix, x, y);
            }
        }

        //Generate Rivers
        grid = GenerateTileRivers(grid, validRiverPoints);

        noiseImage.Unlock();
        return grid;
    }

    private TileTypes CalcTempurature(TileTypes originalTile, Color pix, int x, int y)
    {
        //Temp
        int tempGradient = Math.Abs(y - (noiseImage.GetHeight() / 2));
        float tempGradientNormalized = tempGradient / (noiseImage.GetHeight() / 2.0f);
        //Not water or mountain - Desert
        if (pix.r >= desertLandThreshold && pix.r < desertLandCutoff)
        {
            //Desert
            if (tempGradientNormalized - pix.r / noiseInfluenceDivisorOnTempurature < desertTempuratureCutoff)
            {
                return TileTypes.DesertLand;
            }
        }
        else if (pix.r >= desertLandCutoff)
        {
            //Desert
            if (tempGradientNormalized - pix.r / noiseInfluenceDivisorOnTempurature < desertTempuratureCutoff)
            {
                return TileTypes.DesertMountain;
            }
        }

        //Not water or mountain - Snow
        if (pix.r >= iceBeachThreshold && pix.r < iceBeachCutoff)
        {
            if (tempGradientNormalized - pix.r / noiseInfluenceDivisorOnTempurature > iceTempuratureCutoff)
            {
                return TileTypes.IceBeach;
            }
        }
        else if (pix.r >= iceLandThreshold && pix.r < iceLandCutoff)
        {
            if (tempGradientNormalized - pix.r / noiseInfluenceDivisorOnTempurature > iceTempuratureCutoff)
            {
                return TileTypes.IceLand;
            }
        }
        else if (pix.r >= iceLandCutoff)
        {
            if (tempGradientNormalized - pix.r / noiseInfluenceDivisorOnTempurature > iceTempuratureCutoff)
            {
                return TileTypes.IceMountain;
            }
        }

        return originalTile;
    }

    private int[,] GenerateTileRivers(int[,] grid, List<Vector2> validRiverPoints)
    {
        //Try for generating rviers
        int madeRivers = 0;
        int attemptedRivers = 0;

        while (attemptedRivers < 100000 && madeRivers < maxRiverCount)
        {
            attemptedRivers++;
            //pick random point on from valid points
            Vector2 point = validRiverPoints[rng.RandiRange(0, validRiverPoints.Count - 1)];
            int rX = (int)point.x;
            int rY = (int)point.y;
            Color c = noiseImage.GetPixel(rX, rY);

            //Check if point is on mountain
            if (c.r >= landCutoff)
            {
                Dictionary<Vector2, bool> riveredSpots = new Dictionary<Vector2, bool>();
                //Make River
                int currentX = rX;
                int currentY = rY;
                int tempGradient = Math.Abs(currentY - (noiseImage.GetHeight() / 2));
                float tempGradientNormalized = tempGradient / (noiseImage.GetHeight() / 2.0f);
                Color currentCell = c;

                //Desert
                if (tempGradientNormalized - currentCell.r / noiseInfluenceDivisorOnTempurature < desertTempuratureCutoff)
                {
                    int chance = rng.RandiRange(0, 100);
                    if (chance > percentChanceOfRiverInDesert) continue;
                }
                //Snow
                if (tempGradientNormalized - currentCell.r / noiseInfluenceDivisorOnTempurature > iceTempuratureCutoff)
                {
                    int chance = rng.RandiRange(0, 100);
                    if (chance > percentChanceOfRiverInSnow) continue;
                }

                int attemptsOnRiver = 0;
                while (currentCell.r > seaCutoff && attemptsOnRiver < 1000000)
                {
                    Vector2 currentSpot = new Vector2(currentX, currentY);
                    if (!riveredSpots.ContainsKey(currentSpot))
                        riveredSpots.Add(currentSpot, true);

                    grid[currentX, currentY] = (int)TileTypes.Sea;
                    float recordLow = float.MaxValue;
                    int nX = currentX;
                    int nY = currentY;
                    bool foundNewLowest = false;
                    //check neighbors and wrap around world:
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {

                            //dont check diagonals
                            if (Math.Abs(i) + Math.Abs(j) == 2) continue;
                            //don't check current spot
                            if (Math.Abs(i) + Math.Abs(j) == 0) continue;

                            int checkX = currentX + i;
                            int checkY = currentY + j;

                            //wrap cords
                            if (checkX < 0) checkX = noiseImage.GetWidth() - 1;
                            if (checkX > noiseImage.GetWidth() - 1) checkX = 0;
                            if (checkY < 0) checkY = noiseImage.GetHeight() - 1;
                            if (checkY > noiseImage.GetHeight() - 1) checkY = 0;

                            //Wide Rivers
                            grid[checkX, checkY] = (int)TileTypes.Sea;

                            //Find next Lowest

                            Color checkC = noiseImage.GetPixel(checkX, checkY);
                            if (checkC.r <= recordLow && !riveredSpots.ContainsKey(new Vector2(checkX, checkY)))
                            {
                                recordLow = checkC.r;
                                nX = checkX;
                                nY = checkY;
                                currentCell = checkC;
                                foundNewLowest = true;
                            }
                        }
                    }
                    if (!foundNewLowest)
                    {
                        riveredSpots[currentSpot] = false;
                        //Got Stuck branch from somewhere
                        //Get all points that we are not stuck at

                        List<Vector2> goodSpots = new List<Vector2>();
                        foreach (KeyValuePair<Vector2, bool> spot in riveredSpots)
                        {
                            if (spot.Value == true) goodSpots.Add(spot.Key);
                        }

                        //Pick a random good spot to branch from
                        if (goodSpots.Count == 0) break;

                        float nextLowest = float.MaxValue;
                        foreach (Vector2 spot in goodSpots)
                        {
                            float thisHieght = noiseImage.GetPixel((int)spot.x, (int)spot.y).r;
                            if (thisHieght < nextLowest)
                            {
                                nextLowest = thisHieght;
                                nX = (int)spot.x;
                                nY = (int)spot.y;
                            }
                        }
                        currentCell = noiseImage.GetPixel(nX, nY);
                    }
                    currentX = nX;
                    currentY = nY;
                    attemptsOnRiver++;
                    if (attemptsOnRiver >= 1000000)
                    {
                        attemptsOnRiver = 0;
                        break;
                    };
                }
                madeRivers++;
            }
        }
        return grid;
    }

    public Image GeneratePixelImageFromTiles()
    {
        Image result = new Image();
        result.CopyFrom(noiseImage);
        result.Lock();
        for (int x = 0; x < result.GetWidth(); x++)
        {
            for (int y = 0; y < result.GetHeight(); y++)
            {
                result.SetPixel(x, y, tileColors[(TileTypes)tileGrid[x, y]]);
            }
        }
        result.Unlock();
        return result;
    }

    public WorldGenerator()
    {
        InitVariables();
        GenerateTileGridFromSimplexNoise();
    }
}