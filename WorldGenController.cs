using Godot;
using System;
using System.Collections.Generic;

public class WorldGenController : Node2D
{
    MapInputHandler generatedWroldDisplay;

    Control settingsMenu;
    Control settingsContainer;
    Control splashScreen;
    Control loadingScreen;
    FileDialog exportWindow;

    WorldGenerator worldGenerator;

    Button regenerateBtn;
    Button settingsBtn;
    Button exportBtn;

    Thread genThread;
    Thread exportThread;
    bool exportConfirmed = false;

    List<Label> valueLabels;
    List<HSlider> valueSliders;
    List<ColorPickerButton> valueColors;
    TextEdit valueSeedTextEdit;
    Button valueSeedSet;
    Button valueSeedRandomize;
    public override void _Ready()
    {
        generatedWroldDisplay = GetNode<MapInputHandler>("Node/Sprite");
        settingsMenu = GetNode<Control>("UI/SettingsMenu");
        settingsContainer = GetNode<Control>("UI/SettingsMenu/SettingsMenuContainer/VSplitContainer/ScrollContainer/SettingsSplitter/SettingsContainer");
        splashScreen = GetNode<Control>("UI/SplashScreen");
        loadingScreen = GetNode<Control>("UI/LoadingScreen");

        regenerateBtn = GetNode<Button>("UI/BtnContainer/RegenerateBtn");
        regenerateBtn.Connect("pressed", this, nameof(Regenerate));

        settingsBtn = GetNode<Button>("UI/BtnContainer/SettingsBtn");
        settingsBtn.Connect("pressed", this, nameof(OpenSettings));

        exportBtn = GetNode<Button>("UI/BtnContainer/ExportBtn");
        exportBtn.Connect("pressed", this, nameof(ExportMap));

        exportWindow = GetNode<FileDialog>("UI/FileDialog");
        exportWindow.Connect("file_selected", this, nameof(ExportConfirmed));

        worldGenerator = new WorldGenerator();

        genThread = new Thread();
        exportThread = new Thread();
        genThread.Start(this, nameof(ThreadGen));

        SetupSettings();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        if (Input.IsActionJustPressed("close_settings"))
        {
            generatedWroldDisplay.processInput = true;
            settingsMenu.Visible = false;
        }
    }

    private void OpenSettings()
    {
        if (exportThread.IsAlive()) return;
        generatedWroldDisplay.processInput = false;
        settingsMenu.Visible = true;
    }

    private void Regenerate()
    {
        if (genThread.IsAlive() || exportThread.IsAlive()) return;
        loadingScreen.Visible = true;
        genThread.Start(this, nameof(ThreadGen));
    }

    private void ThreadGen()
    {
        worldGenerator.Regenerate();
        ImageTexture texture = new ImageTexture();
        texture.CreateFromImage(worldGenerator.GeneratePixelImageFromTiles());
        generatedWroldDisplay.Texture = texture;
        CallDeferred(nameof(GenThreadDone));
    }

    private void GenThreadDone()
    {
        loadingScreen.Visible = false;
        splashScreen.Visible = false;
        genThread.WaitToFinish();
    }

    private void ExportMap()
    {
        generatedWroldDisplay.processInput = false;
        if (genThread.IsAlive() || exportThread.IsAlive()) return;
        exportThread.Start(this, nameof(ThreadExport));
    }

    private void ThreadExport()
    {
        exportWindow.PopupCentered();
        while (exportWindow.Visible) { OS.DelayMsec(3); }
        CallDeferred(nameof(ExportThreadDone));
    }

    private void ExportThreadDone()
    {
        exportConfirmed = false;
        generatedWroldDisplay.processInput = true;
        exportThread.WaitToFinish();
    }

    private void ExportConfirmed(string path)
    {
        Console.WriteLine(path);
        try
        {
            Image i = generatedWroldDisplay.Texture.GetData();
            Console.WriteLine(i.SavePng(path));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        exportConfirmed = true;
    }

    private void SetupSettings()
    {
        valueLabels = new List<Label>();
        valueSliders = new List<HSlider>();
        valueColors = new List<ColorPickerButton>();
        foreach (Control item in settingsContainer.GetChildren())
        {
            /*if (item.Name.ToLower().Contains("setting") && item is Label)
            {
                Label thisItem = item as Label;
                Console.WriteLine(thisItem.Name + " is for " + thisItem.Text);
            }*/

            if (item.Name.ToLower().Contains("value") && item is Label)
            {
                Label thisItem = item as Label;
                valueLabels.Add(thisItem);
            }

            if (item.Name.ToLower().Contains("slider") && item is HSlider)
            {
                HSlider thisItem = item as HSlider;
                valueSliders.Add(thisItem);
            }

            if (item.Name.ToLower().Contains("picker") && item is ColorPickerButton)
            {
                ColorPickerButton thisItem = item as ColorPickerButton;
                valueColors.Add(thisItem);
            }

            if (item.Name.ToLower().Contains("value") && item is TextEdit)
            {
                TextEdit thisItem = item as TextEdit;
                valueSeedTextEdit = thisItem;
            }

            if (item.Name.ToLower().Contains("buttoncontainer") && item is HSplitContainer)
            {
                Button b1 = item.GetChild<Button>(0);
                valueSeedSet = b1;
                Button b2 = item.GetChild<Button>(1);
                valueSeedRandomize = b2;
            }
        }

        SetSettingsToWorldGeneratrorValues();
        WireUI();
    }

    private void SetSettingsToWorldGeneratrorValues()
    {
        //Cutoffs
        valueLabels[0].Text = worldGenerator.deepSeaCutoff.ToString();
        valueSliders[0].Value = worldGenerator.deepSeaCutoff;
        valueLabels[1].Text = worldGenerator.seaCutoff.ToString();
        valueSliders[1].Value = worldGenerator.seaCutoff;
        valueLabels[2].Text = worldGenerator.beachCutoff.ToString();
        valueSliders[2].Value = worldGenerator.beachCutoff;
        valueLabels[3].Text = worldGenerator.iceBeachCutoff.ToString();
        valueSliders[3].Value = worldGenerator.iceBeachCutoff;
        valueLabels[4].Text = worldGenerator.landCutoff.ToString();
        valueSliders[4].Value = worldGenerator.landCutoff;
        valueLabels[5].Text = worldGenerator.desertLandCutoff.ToString();
        valueSliders[5].Value = worldGenerator.desertLandCutoff;
        valueLabels[6].Text = worldGenerator.iceLandCutoff.ToString();
        valueSliders[6].Value = worldGenerator.iceLandCutoff;
        valueLabels[7].Text = worldGenerator.mountainCutoff.ToString();
        valueSliders[7].Value = worldGenerator.mountainCutoff;
        valueLabels[8].Text = worldGenerator.snowCutoff.ToString();
        valueSliders[8].Value = worldGenerator.snowCutoff;
        valueLabels[9].Text = worldGenerator.forestCutoff.ToString();
        valueSliders[9].Value = worldGenerator.forestCutoff;
        valueLabels[10].Text = (1 - worldGenerator.desertTempuratureCutoff).ToString();
        valueSliders[10].Value = (1 - worldGenerator.desertTempuratureCutoff);
        valueLabels[11].Text = (1 - worldGenerator.iceTempuratureCutoff).ToString();
        valueSliders[11].Value = (1 - worldGenerator.iceTempuratureCutoff);


        //Thresholds
        valueLabels[12].Text = worldGenerator.forestThreshold.ToString();
        valueSliders[12].Value = worldGenerator.forestThreshold;
        valueLabels[13].Text = worldGenerator.iceBeachThreshold.ToString();
        valueSliders[13].Value = worldGenerator.iceBeachThreshold;
        valueLabels[14].Text = worldGenerator.desertLandThreshold.ToString();
        valueSliders[14].Value = worldGenerator.desertLandThreshold;
        valueLabels[15].Text = worldGenerator.iceLandThreshold.ToString();
        valueSliders[15].Value = worldGenerator.iceLandThreshold;

        //Limits / ETC
        valueLabels[16].Text = worldGenerator.noiseInfluenceDivisorOnTempurature.ToString();
        valueSliders[16].Value = worldGenerator.noiseInfluenceDivisorOnTempurature;
        valueLabels[17].Text = worldGenerator.maxRiverCount.ToString();
        valueSliders[17].Value = worldGenerator.maxRiverCount;
        valueLabels[18].Text = worldGenerator.percentChanceOfRiverInDesert.ToString();
        valueSliders[18].Value = worldGenerator.percentChanceOfRiverInDesert;
        valueLabels[19].Text = worldGenerator.percentChanceOfRiverInSnow.ToString();
        valueSliders[19].Value = worldGenerator.percentChanceOfRiverInSnow;
        valueLabels[20].Text = worldGenerator.octaves.ToString();
        valueSliders[20].Value = worldGenerator.octaves;
        valueLabels[21].Text = worldGenerator.period.ToString();
        valueSliders[21].Value = worldGenerator.period;
        valueLabels[22].Text = worldGenerator.persistance.ToString();
        valueSliders[22].Value = worldGenerator.persistance;
        valueLabels[23].Text = worldGenerator.lacunarity.ToString();
        valueSliders[23].Value = worldGenerator.lacunarity;

        //Seed
        valueSeedTextEdit.Text = worldGenerator.seed.ToString();

        //Colors
        valueColors[0].Color = worldGenerator.deepSeaColor;
        valueColors[1].Color = worldGenerator.seaColor;
        valueColors[2].Color = worldGenerator.beachColor;
        valueColors[3].Color = worldGenerator.iceBeachColor;
        valueColors[4].Color = worldGenerator.landColor;
        valueColors[5].Color = worldGenerator.desertLandColor;
        valueColors[6].Color = worldGenerator.iceLandColor;
        valueColors[7].Color = worldGenerator.mountainColor;
        valueColors[8].Color = worldGenerator.desertMountainColor;
        valueColors[9].Color = worldGenerator.iceMountainColor;
        valueColors[10].Color = worldGenerator.snowColor;
        valueColors[11].Color = worldGenerator.forestColor;
    }

    private void WireUI()
    {
        foreach (HSlider item in valueSliders)
        {
            item.Connect("value_changed", this, nameof(SetWorldGeneratorToSettings));
        }
        foreach (ColorPickerButton item in valueColors)
        {
            item.Connect("color_changed", this, nameof(SetWorldGeneratorToSettings));
        }
        valueSeedSet.Connect("pressed", this, nameof(SetWorldGeneratorToSettings));
        valueSeedRandomize.Connect("pressed", this, nameof(SetWorldGeneratorToSettings));
    }

    private void SetWorldGeneratorToSettings(float value) { SetWorldGeneratorToSettings(); }
    private void SetWorldGeneratorToSettings(Color value) { SetWorldGeneratorToSettings(); }

    private void SetWorldGeneratorToSettings()
    {
        //Cutoffs
        valueLabels[0].Text = valueSliders[0].Value.ToString();
        worldGenerator.deepSeaCutoff = (float)valueSliders[0].Value;
        valueLabels[1].Text = valueSliders[1].Value.ToString();
        worldGenerator.seaCutoff = (float)valueSliders[1].Value;
        valueLabels[2].Text = valueSliders[2].Value.ToString();
        worldGenerator.beachCutoff = (float)valueSliders[2].Value;
        valueLabels[3].Text = valueSliders[3].Value.ToString();
        worldGenerator.iceBeachCutoff = (float)valueSliders[3].Value;
        valueLabels[4].Text = valueSliders[4].Value.ToString();
        worldGenerator.landCutoff = (float)valueSliders[4].Value;
        valueLabels[5].Text = valueSliders[5].Value.ToString();
        worldGenerator.desertLandCutoff = (float)valueSliders[5].Value;
        valueLabels[6].Text = valueSliders[6].Value.ToString();
        worldGenerator.iceLandCutoff = (float)valueSliders[6].Value;
        valueLabels[7].Text = valueSliders[7].Value.ToString();
        worldGenerator.mountainCutoff = (float)valueSliders[7].Value;
        valueLabels[8].Text = valueSliders[8].Value.ToString();
        worldGenerator.snowCutoff = (float)valueSliders[8].Value;
        valueLabels[9].Text = valueSliders[9].Value.ToString();
        worldGenerator.forestCutoff = (float)valueSliders[9].Value;
        valueLabels[10].Text = valueSliders[10].Value.ToString();
        worldGenerator.desertTempuratureCutoff = 1 - (float)valueSliders[10].Value;
        valueLabels[11].Text = valueSliders[11].Value.ToString();
        worldGenerator.iceTempuratureCutoff = 1 - (float)valueSliders[11].Value;

        //Thresholds
        valueLabels[12].Text = valueSliders[12].Value.ToString();
        worldGenerator.forestThreshold = (float)valueSliders[12].Value;
        valueLabels[13].Text = valueSliders[13].Value.ToString();
        worldGenerator.iceBeachThreshold = (float)valueSliders[13].Value;
        valueLabels[14].Text = valueSliders[14].Value.ToString();
        worldGenerator.desertLandThreshold = (float)valueSliders[14].Value;
        valueLabels[15].Text = valueSliders[15].Value.ToString();
        worldGenerator.iceLandThreshold = (float)valueSliders[15].Value;

        //Limits / ETC
        valueLabels[16].Text = valueSliders[16].Value.ToString();
        worldGenerator.noiseInfluenceDivisorOnTempurature = (float)valueSliders[16].Value;
        valueLabels[17].Text = valueSliders[17].Value.ToString();
        worldGenerator.maxRiverCount = (int)valueSliders[17].Value;
        valueLabels[18].Text = valueSliders[18].Value.ToString();
        worldGenerator.percentChanceOfRiverInDesert = (float)valueSliders[18].Value;
        valueLabels[19].Text = valueSliders[19].Value.ToString();
        worldGenerator.percentChanceOfRiverInSnow = (float)valueSliders[19].Value;
        valueLabels[20].Text = valueSliders[20].Value.ToString();

        //Noise
        worldGenerator.octaves = (int)valueSliders[20].Value;
        valueLabels[21].Text = valueSliders[21].Value.ToString();
        worldGenerator.period = (float)valueSliders[21].Value;
        valueLabels[22].Text = valueSliders[22].Value.ToString();
        worldGenerator.persistance = (float)valueSliders[22].Value;
        valueLabels[23].Text = valueSliders[23].Value.ToString();
        worldGenerator.lacunarity = (float)valueSliders[23].Value;

        //Seed
        int seedhash = (int)valueSeedTextEdit.Text.Hash();
        valueSeedTextEdit.Text = seedhash.ToString();
        worldGenerator.SetSeed((ulong)seedhash);

        //Colors
        worldGenerator.deepSeaColor = valueColors[0].Color;
        worldGenerator.seaColor = valueColors[1].Color;
        worldGenerator.beachColor = valueColors[2].Color;
        worldGenerator.iceBeachColor = valueColors[3].Color;
        worldGenerator.landColor = valueColors[4].Color;
        worldGenerator.desertLandColor = valueColors[5].Color;
        worldGenerator.iceLandColor = valueColors[6].Color;
        worldGenerator.mountainColor = valueColors[7].Color;
        worldGenerator.desertMountainColor = valueColors[8].Color;
        worldGenerator.iceMountainColor = valueColors[9].Color;
        worldGenerator.snowColor = valueColors[10].Color;
        worldGenerator.forestColor = valueColors[11].Color;
    }
}
