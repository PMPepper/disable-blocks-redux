using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Planet;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace DisableBlocks
{
    public enum StaticBlockCategory
    {
        LightArmor,
        HeavyArmor,
        Beams,
        InteriorBlocks,
        CoverBlocks,
        NeonTubes,
        Shelves,
        StaticWheels,
        SubgridHeads,
        BlastDoors,
        SteelCatwalks,
        Passage,
        WindowsNonglass,
        WindowsGlass,
        Stairs,
        Railings,
        GratedCatwalk,
        Conveyors,
        LetterNumberBlocks,
        DeadEngineers
    }

    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class DisableBlocksCore : MySessionComponentBase
    {
        public static DisableBlocksCore Instance;

        private const string CONFIG_FILE_NAME = "Config.xml";
        public static DisableBlocksSettingsConfig ConfigData;

        public static readonly MyDefinitionId nuclearMissileAmmoId = MyVisualScriptLogicProvider.GetDefinitionId("AmmoMagazine", "Missile200mm_Nuclear");

        public override void LoadData()
        {
            if (MyAPIGateway.Session.IsServer)
            {
                if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(CONFIG_FILE_NAME, typeof(DisableBlocksSettingsConfig)))
                {
                    MyAPIGateway.Parallel.Start(() =>
                    {
                        var config = new DisableBlocksSettingsConfig();
                        config.Populate();
                        using (var sw = MyAPIGateway.Utilities.WriteFileInWorldStorage(CONFIG_FILE_NAME,
                            typeof(DisableBlocksSettingsConfig))) sw.Write(MyAPIGateway.Utilities.SerializeToXML<DisableBlocksSettingsConfig>(config));
                    });
                }

                try
                {
                    ConfigData = null;
                    var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(CONFIG_FILE_NAME, typeof(DisableBlocksSettingsConfig));
                    string configcontents = reader.ReadToEnd();
                    ConfigData = MyAPIGateway.Utilities.SerializeFromXML<DisableBlocksSettingsConfig>(configcontents);

                    byte[] bytes = MyAPIGateway.Utilities.SerializeToBinary(ConfigData);
                    string encodedConfig = Convert.ToBase64String(bytes);

                    MyAPIGateway.Utilities.SetVariable("DisableBlocksSettings_Config_xml", encodedConfig);

                    //MyLog.Default.WriteLineAndConsole($"EXPANDED!: " + encodedConfig);
                }
                catch (Exception exc)
                {
                    ConfigData = new DisableBlocksSettingsConfig();
                    ConfigData.Populate();
                    //MyLog.Default.WriteLineAndConsole($"ERROR: {exc.Message} : {exc.StackTrace} : {exc.InnerException}");
                }
            }
            else
            {
                try
                {
                    string str;
                    MyAPIGateway.Utilities.GetVariable("DisableBlocksSettings_Config_xml", out str);

                    byte[] bytes = Convert.FromBase64String(str);
                    ConfigData = MyAPIGateway.Utilities.SerializeFromBinary<DisableBlocksSettingsConfig>(bytes);
                }
                catch
                {
                    ConfigData = new DisableBlocksSettingsConfig();
                    ConfigData.Populate();
                }
            }

            foreach (var defConfigGroup in ConfigData.FunctionalBlockConfigs)
            {
                foreach (var defConfig in defConfigGroup.BlockConfigs)
                {
                    if (!defConfig.CanBuild)
                    {
                        var groupId = MyDefinitionManager.Static.TryGetDefinitionGroup(defConfig.Type);
                        if (groupId != null)
                        {
                            if (groupId.Large != null)
                            {
                                groupId.Large.AvailableInSurvival = false;
                            }

                            if (groupId.Small != null)
                            {
                                groupId.Small.AvailableInSurvival = false;
                            }

                            if (!string.IsNullOrEmpty(groupId.Any.MirroringBlock))
                            {
                                var mirrorGroupId = MyDefinitionManager.Static.TryGetDefinitionGroup(groupId.Any.MirroringBlock);
                                if (mirrorGroupId != null)
                                {
                                    if (mirrorGroupId.Large != null)
                                    {
                                        mirrorGroupId.Large.AvailableInSurvival = false;
                                    }

                                    if (mirrorGroupId.Small != null)
                                    {
                                        mirrorGroupId.Small.AvailableInSurvival = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            foreach (var defConfigGroup in ConfigData.ModBlockConfigs)
            {
                foreach (var defConfig in defConfigGroup.BlockConfigs)
                {
                    if (!defConfig.CanBuild)
                    {
                        var groupId = MyDefinitionManager.Static.TryGetDefinitionGroup(defConfig.Type);
                        if (groupId != null)
                        {
                            if (groupId.Large != null)
                            {
                                groupId.Large.AvailableInSurvival = false;
                            }

                            if (groupId.Small != null)
                            {
                                groupId.Small.AvailableInSurvival = false;
                            }
                        }
                    }
                }
            }

            foreach (var defConfig in ConfigData.NonFunctionalBlockGroupConfigs)
            {
                if (!defConfig.CanBuild)
                {
                    StaticBlockCategory staticBlockCategory;
                    if (Enum.TryParse(defConfig.Type, out staticBlockCategory))
                    {
                        switch (staticBlockCategory)
                        {
                            case StaticBlockCategory.LightArmor:
                                var descLight = MyStringId.GetOrCompute("Description_LightArmor");
                                var lightArmorDefs = MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>().Where((def) => { return def.Context.IsBaseGame && def.DescriptionEnum == descLight; });
                                foreach (var def in lightArmorDefs)
                                    def.AvailableInSurvival = false;
                                break;
                            case StaticBlockCategory.HeavyArmor:
                                var descHeavy = MyStringId.GetOrCompute("Description_HeavyArmor");
                                var heavyArmorDefs = MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>().Where((def) => { return def.Context.IsBaseGame && def.DescriptionEnum == descHeavy; });
                                foreach (var def in heavyArmorDefs)
                                    def.AvailableInSurvival = false;
                                break;
                            case StaticBlockCategory.Beams:
                                var descBeam = MyStringId.GetOrCompute("Description_BeamBlock");
                                var beamDefs = MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>().Where((def) => { return def.Context.IsBaseGame && def.DescriptionEnum == descBeam; });
                                foreach (var def in beamDefs)
                                    def.AvailableInSurvival = false;
                                break;
                            case StaticBlockCategory.InteriorBlocks:
                                var descPillar = MyStringId.GetOrCompute("Description_InteriorPillar");
                                var descInterior = MyStringId.GetOrCompute("Description_InteriorWall");
                                var pillarDefs = MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>().Where((def) => { return def.Context.IsBaseGame && (def.DescriptionEnum == descPillar || def.DescriptionEnum == descInterior); });
                                foreach (var def in pillarDefs)
                                    def.AvailableInSurvival = false;
                                break;
                            case StaticBlockCategory.CoverBlocks:
                                var descSetCover = new HashSet<MyStringId>();
                                descSetCover.Add(MyStringId.GetOrCompute("Description_FullCoverWall"));
                                descSetCover.Add(MyStringId.GetOrCompute("Description_FireCover"));
                                descSetCover.Add(MyStringId.GetOrCompute("Description_Embrasure"));
                                var coverDefs = MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>().Where((def) => { return def.Context.IsBaseGame && descSetCover.Contains(def.DescriptionEnum.GetValueOrDefault()); });
                                foreach (var def in coverDefs)
                                    def.AvailableInSurvival = false;
                                break;
                            case StaticBlockCategory.NeonTubes:
                                var descNeon = MyStringId.GetOrCompute("Description_NeonTubes");
                                var neonDefs = MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>().Where((def) => { return def.Context.IsBaseGame && def.DescriptionEnum == descNeon; });
                                foreach (var def in neonDefs)
                                    def.AvailableInSurvival = false;
                                break;
                            case StaticBlockCategory.Shelves:
                                var descShelf = MyStringId.GetOrCompute("Description_StorageShelf");
                                var shelfDefs = MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>().Where((def) => { return def.Context.IsBaseGame && def.DescriptionEnum == descShelf; });
                                foreach (var def in shelfDefs)
                                    def.AvailableInSurvival = false;
                                break;
                            case StaticBlockCategory.StaticWheels:
                                var descWheel = MyStringId.GetOrCompute("Description_Wheel");
                                var wheelDefs = MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>().Where((def) => { return def.Context.IsBaseGame && def.DescriptionEnum == descWheel; });
                                foreach (var def in wheelDefs)
                                    def.AvailableInSurvival = false;
                                break;
                            case StaticBlockCategory.SubgridHeads:
                                var descSetSubHead = new HashSet<MyStringId>();
                                descSetSubHead.Add(MyStringId.GetOrCompute("Description_AdvancedRotorPart"));
                                descSetSubHead.Add(MyStringId.GetOrCompute("Description_RotorPart"));
                                descSetSubHead.Add(MyStringId.GetOrCompute("Description_PistonTop"));
                                descSetSubHead.Add(MyStringId.GetOrCompute("Description_HingeHead"));
                                var subHeadDefs = MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>().Where((def) => { return def.Context.IsBaseGame && descSetSubHead.Contains(def.DescriptionEnum.GetValueOrDefault()); });
                                foreach (var def in subHeadDefs)
                                    def.AvailableInSurvival = false;
                                break;
                            case StaticBlockCategory.BlastDoors:
                                var descBlast = MyStringId.GetOrCompute("Description_BlastDoor");
                                var blastDefs = MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>().Where((def) => { return def.Context.IsBaseGame && def.DescriptionEnum == descBlast; });
                                foreach (var def in blastDefs)
                                    def.AvailableInSurvival = false;
                                break;
                            case StaticBlockCategory.SteelCatwalks:
                                var descSteel = MyStringId.GetOrCompute("Description_SteelCatwalk");
                                var steelDefs = MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>().Where((def) => { return def.Context.IsBaseGame && def.DescriptionEnum == descSteel; });
                                foreach (var def in steelDefs)
                                    def.AvailableInSurvival = false;
                                break;
                            case StaticBlockCategory.Passage:
                                var descPass = MyStringId.GetOrCompute("Description_Passage");
                                var passDefs = MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>().Where((def) => { return def.Context.IsBaseGame && def.DescriptionEnum == descPass; });
                                foreach (var def in passDefs)
                                    def.AvailableInSurvival = false;
                                break;
                            case StaticBlockCategory.WindowsNonglass:
                                var descGlass = MyStringId.GetOrCompute("Description_Window");
                                var descSetNonGlass = new HashSet<MyStringId>();
                                descSetNonGlass.Add(MyStringId.GetOrCompute("Description_BarredWindow"));
                                descSetNonGlass.Add(MyStringId.GetOrCompute("Description_ViewPort"));
                                descSetNonGlass.Add(MyStringId.GetOrCompute("Description_VerticalWindow"));
                                descSetNonGlass.Add(MyStringId.GetOrCompute("Description_DiagonalWindow"));
                                descSetNonGlass.Add(MyStringId.GetOrCompute("Description_WindowWall"));
                                descSetNonGlass.Add(MyStringId.GetOrCompute("Description_WindowWallLeft"));
                                descSetNonGlass.Add(MyStringId.GetOrCompute("Description_WindowWallRight"));
                                var glassDefs = MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>().Where((def) => { return def.Context.IsBaseGame && ((def.DescriptionEnum == descGlass && (def.Id.SubtypeName.StartsWith("Half") || def.Id.SubtypeName.StartsWith("Bridge"))) || descSetNonGlass.Contains(def.DescriptionEnum.GetValueOrDefault())); });
                                foreach (var def in glassDefs)
                                    def.AvailableInSurvival = false;
                                break;
                            case StaticBlockCategory.WindowsGlass:
                                var descGlass2 = MyStringId.GetOrCompute("Description_Window");
                                var glassDefs2 = MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>().Where((def) => { return def.Context.IsBaseGame && def.DescriptionEnum == descGlass2 && !(def.Id.SubtypeName.StartsWith("Half") || def.Id.SubtypeName.StartsWith("Bridge")); });
                                foreach (var def in glassDefs2)
                                    def.AvailableInSurvival = false;
                                break;
                            case StaticBlockCategory.Stairs:
                                var descSetStair = new HashSet<MyStringId>();
                                descSetStair.Add(MyStringId.GetOrCompute("Description_GratedStairs"));
                                descSetStair.Add(MyStringId.GetOrCompute("Description_GratedHalfStairs"));
                                descSetStair.Add(MyStringId.GetOrCompute("Description_Stairs"));
                                descSetStair.Add(MyStringId.GetOrCompute("Description_Ramp"));
                                var stairDefs = MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>().Where((def) => { return def.Context.IsBaseGame && descSetStair.Contains(def.DescriptionEnum.GetValueOrDefault()); });
                                foreach (var def in stairDefs)
                                    def.AvailableInSurvival = false;
                                break;
                            case StaticBlockCategory.Railings:
                                var railingDefs = MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>().Where((def) => { return def.Context.IsBaseGame && def.DescriptionEnum.GetValueOrDefault().String.StartsWith("Description_Railing"); });
                                foreach (var def in railingDefs)
                                    def.AvailableInSurvival = false;
                                break;
                            case StaticBlockCategory.GratedCatwalk:
                                var gratedDefs = MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>().Where((def) => { return def.Context.IsBaseGame && def.DescriptionEnum.GetValueOrDefault().String.StartsWith("Description_GratedCatwalk"); });
                                foreach (var def in gratedDefs)
                                    def.AvailableInSurvival = false;
                                break;
                            case StaticBlockCategory.Conveyors:
                                var descConv = MyStringId.GetOrCompute("Description_ConveyorTube");
                                var convDefs = MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>().Where((def) => { return def.Context.IsBaseGame && def.DescriptionEnum == descConv; });
                                foreach (var def in convDefs)
                                    def.AvailableInSurvival = false;
                                break;
                            case StaticBlockCategory.LetterNumberBlocks:
                                var descSetSym = new HashSet<MyStringId>();
                                descSetSym.Add(MyStringId.GetOrCompute("Description_Letters"));
                                descSetSym.Add(MyStringId.GetOrCompute("Description_Numbers"));
                                descSetSym.Add(MyStringId.GetOrCompute("Description_Symbols"));
                                var letterDefs = MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>().Where((def) => { return def.Context.IsBaseGame && descSetSym.Contains(def.DescriptionEnum.GetValueOrDefault()); });
                                foreach (var def in letterDefs)
                                    def.AvailableInSurvival = false;
                                break;
                            case StaticBlockCategory.DeadEngineers:
                                var descDead = MyStringId.GetOrCompute("Description_DeadEngineer");
                                var deadDefs = MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>().Where((def) => { return def.Context.IsBaseGame && def.DescriptionEnum == descDead; });
                                foreach (var def in deadDefs)
                                    def.AvailableInSurvival = false;
                                break;
                        }
                    }
                }
            }
        }

        protected override void UnloadData()
        {
            
        }

        public override void BeforeStart()
        {
            Instance = this;

            MyDefinitionId freightId = MyVisualScriptLogicProvider.GetDefinitionId("CubeBlock", "Freight1");
            var ddd = MyDefinitionManager.Static.GetCubeBlockDefinition(freightId);
            MyLog.Default.WriteLineAndConsole("PAIR!!! " + ddd.BlockPairName);
        }

        public override void UpdateBeforeSimulation()
        {

        }
    }
}