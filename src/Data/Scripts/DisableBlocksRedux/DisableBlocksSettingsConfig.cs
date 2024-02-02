using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace DisableBlocks
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class DisableBlocksSettingsConfig
    {
        [ProtoMember(1)]
        public List<BlockConfig> NonFunctionalBlockGroupConfigs = new List<BlockConfig>();
        [ProtoMember(2)]
        public List<BlockGroupConfig> FunctionalBlockConfigs = new List<BlockGroupConfig>();
        [ProtoMember(3)]
        public List<BlockGroupConfig> ModBlockConfigs = new List<BlockGroupConfig>();

        public void Populate()
        {
            Dictionary<string, List<BlockConfig>> locBlocksConfigTable = new Dictionary<string, List<BlockConfig>>();
            Dictionary<string, List<BlockConfig>> locModBlocksConfigTable = new Dictionary<string, List<BlockConfig>>();
            List<BlockConfig> locModBlockConfigs = new List<BlockConfig>();
            HashSet<string> groupIds = new HashSet<string>();

            MyDefinitionId basicAssemblerId = MyVisualScriptLogicProvider.GetDefinitionId("Assembler", "BasicAssembler");
            var funcBlockDefs = new HashSet<MyCubeBlockDefinition>(MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>().Where((def) => { return def is MyFunctionalBlockDefinition || def is MyWarheadDefinition; }));

            foreach (var block in funcBlockDefs)
            {
                if (block.Public && block.Id != basicAssemblerId)
                {
                    if (!groupIds.Contains(block.BlockPairName))
                    {
                        var blockGroup = MyDefinitionManager.Static.TryGetDefinitionGroup(block.BlockPairName);
                        if (blockGroup != null)
                        {
                            groupIds.Add(block.BlockPairName);
                            if (!string.IsNullOrEmpty(block.MirroringBlock))
                                groupIds.Add(block.MirroringBlock);
                            if (blockGroup.Any != null)
                            {
                                if (blockGroup.Any.Context.IsBaseGame)
                                {
                                    var key = block.Id.TypeId.ToString();
                                    if (!locBlocksConfigTable.ContainsKey(key))
                                        locBlocksConfigTable.Add(key, new List<BlockConfig>());
                                    locBlocksConfigTable[key].Add(new BlockConfig(block.BlockPairName));
                                }
                                else
                                {
                                    var key = blockGroup.Any.Context.ModId;
                                    if (!locModBlocksConfigTable.ContainsKey(key))
                                        locModBlocksConfigTable.Add(key, new List<BlockConfig>());
                                    locModBlocksConfigTable[key].Add(new BlockConfig(block.BlockPairName));
                                }
                            }
                        }
                    }
                }
            }

            if (!locBlocksConfigTable.ContainsKey("MyObjectBuilder_CargoContainer"))
                locBlocksConfigTable.Add("MyObjectBuilder_CargoContainer", new List<BlockConfig>());
            locBlocksConfigTable["MyObjectBuilder_CargoContainer"].Add(new BlockConfig("DisplayName_Block_Freight1"));
            locBlocksConfigTable["MyObjectBuilder_CargoContainer"].Add(new BlockConfig("DisplayName_Block_Freight2"));
            locBlocksConfigTable["MyObjectBuilder_CargoContainer"].Add(new BlockConfig("DisplayName_Block_Freight3"));

            if (!locBlocksConfigTable.ContainsKey("MyObjectBuilder_CubeBlock"))
                locBlocksConfigTable.Add("MyObjectBuilder_CubeBlock", new List<BlockConfig>());
            locBlocksConfigTable["MyObjectBuilder_CubeBlock"].Add(new BlockConfig("DisplayName_Block_Shower"));
            locBlocksConfigTable["MyObjectBuilder_CubeBlock"].Add(new BlockConfig("DeskChairless"));
            locBlocksConfigTable["MyObjectBuilder_CubeBlock"].Add(new BlockConfig("DeskChairlessCorner"));
            locBlocksConfigTable["MyObjectBuilder_CubeBlock"].Add(new BlockConfig("DeskChairlessCornerInv"));
            locBlocksConfigTable["MyObjectBuilder_CubeBlock"].Add(new BlockConfig("EngineerPlushie"));

            if (!locBlocksConfigTable.ContainsKey("MyObjectBuilder_TerminalBlock"))
                locBlocksConfigTable.Add("MyObjectBuilder_TerminalBlock", new List<BlockConfig>());
            locBlocksConfigTable["MyObjectBuilder_TerminalBlock"].Add(new BlockConfig("ControlPanel"));
            locBlocksConfigTable["MyObjectBuilder_TerminalBlock"].Add(new BlockConfig("SciFiTerminal"));

            if (!locBlocksConfigTable.ContainsKey("MyObjectBuilder_Ladder2"))
                locBlocksConfigTable.Add("MyObjectBuilder_Ladder2", new List<BlockConfig>());
            locBlocksConfigTable["MyObjectBuilder_Ladder2"].Add(new BlockConfig("Ladder2"));
            locBlocksConfigTable["MyObjectBuilder_Ladder2"].Add(new BlockConfig("LadderShaft"));

            foreach (var keyValue in locBlocksConfigTable)
            {
                keyValue.Value.Sort(new BlockConfigComparer());
                FunctionalBlockConfigs.Add(new BlockGroupConfig(keyValue.Key, keyValue.Value));
            }

            foreach (var keyValue in locModBlocksConfigTable)
            {
                keyValue.Value.Sort(new BlockConfigComparer());
                ModBlockConfigs.Add(new BlockGroupConfig(keyValue.Key, keyValue.Value));
            }

            locModBlockConfigs.Sort(new BlockConfigComparer());

            foreach (var name in Enum.GetNames(typeof(StaticBlockCategory)))
                NonFunctionalBlockGroupConfigs.Add(new BlockConfig(name));
        }
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class BlockGroupConfig
    {
        [ProtoMember(1)]
        [XmlAttribute("BlockType")]
        public string BlockType;
        [ProtoMember(2)]
        public List<BlockConfig> BlockConfigs = new List<BlockConfig>();

        public BlockGroupConfig() { }

        public BlockGroupConfig(string BlockType, List<BlockConfig> BlockConfigs)
        {
            this.BlockType = BlockType;
            this.BlockConfigs = BlockConfigs;
        }
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class BlockConfig
    {
        [XmlAttribute("Type")]
        public string Type;
        [XmlAttribute("CanBuild")]
        public bool CanBuild = true;

        public BlockConfig() { }

        public BlockConfig(string groupId)
        {
            this.Type = groupId;
        }
    }

    public class BlockConfigComparer : Comparer<BlockConfig>
    {
        public override int Compare(BlockConfig x, BlockConfig y)
        {
            return x.Type.CompareTo(y.Type);
        }
    }
}
