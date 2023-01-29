using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace BBA;

public static partial class Inject
{
    public static bool Initialised { get; internal set; } = false;

    public static TypeDescription CabinetTD { get; internal set; }
    public static TypeDescription BudgetBoyGameTD { get; internal set; }
    public static TypeDescription BudgetBoyFirmwareTD { get; internal set; }
    public static TypeDescription BudgetBoyControlsTD { get; internal set; }
    public static TypeDescription HighscoreTD { get; internal set; }
    public static TypeDescription ButtonTD { get; internal set; }
    public static TypeDescription TitleStageTD { get; internal set; }
    public static TypeDescription MainTD { get; internal set; }

    public static Entity Cabinet { get; internal set; }
    public static object Game { get; internal set; }
    public static object GameControls { get; internal set; }
    public static object GameControlsAButton { get; internal set; }

    [Event.Hotload]
    [Event.Tick.Client] // FIXME: not proud of it, should find some event that is reliably called only once
    public static void LoadClasses()
    {
        if (Initialised)
            return;

        #region Looking for classes

        CabinetTD = TypeLibrary.GetType("Sandbox.Cabinet");

        HighscoreTD = TypeLibrary.GetType("GameAPI.Highscore");
        ButtonTD = TypeLibrary.GetType("GameAPI.Button");

        BudgetBoyGameTD = TypeLibrary.GetType("GameAPI.BudgetBoy.Game");
        BudgetBoyFirmwareTD = TypeLibrary.GetType("GameAPI.BudgetBoy.Firmware");
        BudgetBoyControlsTD = TypeLibrary.GetType("GameAPI.BudgetBoy.Controls");

        TitleStageTD = TypeLibrary.GetType("Games.BlockParty.TitleStage");
        MainTD = TypeLibrary.GetType("Games.BlockParty.Main");

        #endregion

        #region Cabinet
        
        var CabinetType = CabinetTD.TargetType;
        Cabinet = Entity.All.FirstOrDefault(entity => entity.GetType() == CabinetType);
        Log.Info($"Cabinet {Cabinet}");

        #endregion
        
        #region Game
        
        var GameField = CabinetTD.GetProperty("Game");
        Game = GameField.GetValue(Cabinet);
        
        #endregion
        
        #region Controls

        var GameControlsField = BudgetBoyGameTD.GetProperty("Controls");
        GameControls = GameControlsField.GetValue(Game);

        var GameControlsAField = BudgetBoyControlsTD.GetProperty("A");
        GameControlsAButton = GameControlsAField.GetValue(GameControls);
        
        #endregion

        Initialised = true;

        AddHardcore();
    }

    private static void AddHardcore()
    {
        var CurrentStage = GetCurrentStage();
        if (CurrentStage.GetType() != TitleStageTD.TargetType)
        {
            Log.Error($"Called while not at the Title Stage! (stage type: {CurrentStage.GetType()}");
            return;
        }

        var AddNewOptionMethod = TitleStageTD.GetMethod("AddNewOption");
        AddNewOptionMethod.Invoke(CurrentStage, new object[] { "HARDCORE" });
    }

    public static object GetCurrentStage()
    {
        var CurrentStageField = MainTD.GetProperty("CurrentStage");
        return CurrentStageField.GetValue(Game);
    }

    public static bool IsButtonJustPressed(object button)
    {
        var GameControlsButtonJustPressedField = ButtonTD.GetProperty("JustPressed");
        return (bool)GameControlsButtonJustPressedField.GetValue(button);
    }

    [Event.Tick.Client]
    private static void Tick()
    {
        var CurrentStage = GetCurrentStage();
        var CurrentStageType = CurrentStage.GetType();
        var stageTick = new Dictionary<Type, Action>()
        {
            { 
                TitleStageTD.TargetType, () =>
                {
                    if (IsButtonJustPressed(GameControlsAButton))
                    {
                        var TitleStageSelectedOptionField = TitleStageTD.GetProperty("_selectedOption");
                        var _selectedOption = (int)TitleStageSelectedOptionField.GetValue(CurrentStage);
                        Log.Info($"{_selectedOption}");
                    }
                }
            }
        };

        if (stageTick.ContainsKey(CurrentStageType))
            stageTick[CurrentStageType]();
    }

    [ConCmd.Client("add_highscore")]
    public static void AddHighscore(string nickname, int score)
    {
        var FirmwareField = BudgetBoyGameTD.GetProperty("Firmware");
        var Firmware = FirmwareField.GetValue(Game);
        Log.Info($"{Firmware}");

        var AddHighscore = BudgetBoyFirmwareTD.GetMethod("AddHighscore");

        var newHighscore = HighscoreTD.Create<object>(new object[] { nickname, score });
        Log.Info($"{newHighscore}");

        AddHighscore.Invoke(Firmware, new object[] { newHighscore });
    }

    [ConCmd.Client("uninitialize")]
    public static void UnInitialize() => Initialised = false;
}