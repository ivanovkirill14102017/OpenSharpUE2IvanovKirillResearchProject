namespace L2Viewer.DatFile;

public abstract record DatDocument(string Path);

public sealed record SkillNameDatDocument(
    string Path,
    IReadOnlyList<SkillNameDatEntry> Entries) : DatDocument(Path);

public sealed record SkillNameDatEntry(
    int SkillId,
    int SkillLevel,
    string Name,
    string Description,
    string DescriptionAdd1,
    string DescriptionAdd2);

public sealed record SkillGrpCt15DatDocument(
    string Path,
    IReadOnlyList<SkillGrpCt15DatEntry> Entries) : DatDocument(Path);

public sealed record SkillGrpCt15DatEntry(
    int SkillId,
    int SkillLevel,
    int OperType,
    int MpConsume,
    int CastRange,
    int CastStyle,
    int Unknown0,
    float HitTime,
    int IsMagic,
    string AnimationCharacter,
    string DescriptionToken,
    string IconName,
    string IconName2,
    int IsEnchanted,
    int EnchantedSkillId,
    int HpConsume,
    int Unknown1,
    int Unknown2,
    int Unknown3);

public sealed record MobSkillAnimGrpDatDocument(
    string Path,
    IReadOnlyList<MobSkillAnimGrpDatEntry> Entries) : DatDocument(Path);

public sealed record MobSkillAnimGrpDatEntry(
    int NpcId,
    int SkillId,
    string SequenceName,
    string SkillName,
    string NpcName,
    string NpcClass);

public sealed record StaticObjectDatDocument(
    string Path,
    IReadOnlyList<StaticObjectDatEntry> Entries) : DatDocument(Path);

public sealed record StaticObjectDatEntry(
    uint Id,
    string Name);

public sealed record ZoneNameDatDocument(
    string Path,
    IReadOnlyList<ZoneNameDatEntry> Entries) : DatDocument(Path);

public sealed record ZoneNameDatEntry(
    uint Id,
    uint ZoneColorId,
    uint XWorldGrid,
    uint YWorldGrid,
    float TopZ,
    float BottomZ,
    string ZoneName,
    IReadOnlyList<int> Coordinates,
    float Unknown01,
    string Map);

public sealed record HuntingZoneDatDocument(
    string Path,
    IReadOnlyList<HuntingZoneDatEntry> Entries) : DatDocument(Path);

public sealed record HuntingZoneDatEntry(
    uint Id,
    uint HuntingType,
    uint Level,
    uint Unknown1,
    float LocationX,
    float LocationY,
    float LocationZ,
    string Extra,
    uint AffiliatedAreaId,
    string Name);

public sealed record VariationEffectGrpDatDocument(
    string Path,
    IReadOnlyList<VariationEffectGrpDatEntry> Entries) : DatDocument(Path);

public sealed record VariationEffectGrpDatEntry(
    uint Unknown0,
    uint Unknown1,
    uint Unknown2,
    uint Unknown3,
    uint Unknown4,
    string Effect,
    string Attribute);

public sealed record NpcNameDatDocument(
    string Path,
    IReadOnlyList<NpcNameDatEntry> Entries) : DatDocument(Path);

public sealed record NpcNameDatEntry(
    uint Id,
    string Name,
    string Description,
    byte Red,
    byte Green,
    byte Blue,
    byte Reserved1);

public sealed record NpcGrpDatDocument(
    string Path,
    IReadOnlyList<NpcGrpDatEntry> Entries) : DatDocument(Path);

public sealed record NpcGrpDatEntry(
    uint Tag,
    string Class,
    string Mesh,
    IReadOnlyList<string> Textures1,
    IReadOnlyList<string> Textures2,
    IReadOnlyList<uint> DynamicTable1,
    float NpcSpeed,
    IReadOnlyList<string> Unknown0,
    IReadOnlyList<string> Sounds1,
    IReadOnlyList<string> Sounds2,
    IReadOnlyList<string> Sounds3,
    uint RbEffectOn,
    string? RbEffect,
    float? RbEffectScale,
    IReadOnlyList<uint> Unknown1,
    string Effect,
    uint Unknown2,
    float SoundRadius,
    float SoundVolume,
    float SoundRandomness,
    uint QuestBe,
    uint ClassLimit);

public sealed record ActionNameDatDocument(
    string Path,
    IReadOnlyList<ActionNameDatEntry> Entries) : DatDocument(Path);

public sealed record ActionNameDatEntry(
    uint Tag,
    uint Id,
    int Type,
    uint Category,
    IReadOnlyList<int> Categories2,
    string Command,
    string Icon,
    string Name,
    string Description);

public sealed record ItemNameDatDocument(
    string Path,
    IReadOnlyList<ItemNameDatEntry> Entries) : DatDocument(Path);

public sealed record ItemNameDatEntry(
    uint Id,
    string Name,
    string AdditionalName,
    string Description,
    int Popup,
    string SetIds,
    string SetBonusDescription,
    string SetExtraId,
    string SetExtraDescription,
    byte[] Unknown,
    uint SpecialEnchantAmount,
    string SpecialEnchantDescription);

public sealed record SystemMsgDatDocument(
    string Path,
    IReadOnlyList<SystemMsgDatEntry> Entries) : DatDocument(Path);

public sealed record SystemMsgDatEntry(
    uint Id,
    uint Unknown0,
    string Message,
    uint Group,
    byte[] Rgba,
    string ItemSound,
    string SystemMessageReference,
    IReadOnlyList<uint> Unknown1,
    string SubMessage,
    string Type);

public sealed record QuestNameDatDocument(
    string Path,
    IReadOnlyList<QuestNameDatEntry> Entries) : DatDocument(Path);

public sealed record QuestNameDatEntry(
    uint Tag,
    uint QuestId,
    uint QuestProgress,
    string MainName,
    string ProgressName,
    string Description,
    IReadOnlyList<int> ItemIds,
    IReadOnlyList<int> ItemCounts,
    float QuestX,
    float QuestY,
    float QuestZ,
    uint LevelMin,
    uint LevelMax,
    uint QuestType,
    string EntityName,
    uint GetItemInQuest,
    uint Unknown1,
    uint Unknown2,
    uint ContactNpcId,
    float ContactNpcX,
    float ContactNpcY,
    float ContactNpcZ,
    string Restrictions,
    string ShortDescription,
    IReadOnlyList<int> RequiredClassIds,
    IReadOnlyList<int> RequiredItemIds,
    uint ClanPetQuest,
    uint RequiredQuestComplete,
    uint Unknown3,
    uint AreaId);

public sealed record MusicInfoDatDocument(
    string Path,
    IReadOnlyList<MusicInfoDatEntry> Entries) : DatDocument(Path);

public sealed record MusicInfoDatEntry(
    uint Id,
    IReadOnlyList<string> Tracks);

public sealed record CastleNameDatDocument(
    string Path,
    IReadOnlyList<CastleNameDatEntry> Entries) : DatDocument(Path);

public sealed record CastleNameDatEntry(
    uint Number,
    uint Tag,
    uint Id,
    string CastleName,
    string Location,
    string Description);

public sealed record ClassInfoDatDocument(
    string Path,
    IReadOnlyList<ClassInfoDatEntry> Entries) : DatDocument(Path);

public sealed record ClassInfoDatEntry(
    uint Id,
    string Name);

public sealed record CommandNameDatDocument(
    string Path,
    IReadOnlyList<CommandNameDatEntry> Entries) : DatDocument(Path);

public sealed record CommandNameDatEntry(
    uint Number,
    uint Id,
    string Name);

public sealed record CreditGrpDatDocument(
    string Path,
    IReadOnlyList<CreditGrpDatEntry> Entries) : DatDocument(Path);

public sealed record CreditGrpDatEntry(
    uint Id,
    string Html,
    string Image,
    uint Time,
    uint Align);

public sealed record EnterEventGrpDatDocument(
    string Path,
    IReadOnlyList<EnterEventGrpDatEntry> Entries) : DatDocument(Path);

public sealed record EnterEventGrpDatEntry(
    uint Id,
    byte Unknown0,
    string SkillSound,
    float SoundVolume,
    float SoundRadius,
    uint IsRise,
    uint SpawnType,
    string EffectName,
    string AnimationName);

public sealed record EulaDatDocument(
    string Path,
    IReadOnlyList<EulaDatEntry> Entries) : DatDocument(Path);

public sealed record EulaDatEntry(
    string Eula,
    string Footer1,
    string Footer2);

public sealed record GameTipDatDocument(
    string Path,
    IReadOnlyList<GameTipDatEntry> Entries) : DatDocument(Path);

public sealed record GameTipDatEntry(
    uint Id,
    uint Unknown1,
    uint Unknown2,
    uint Enabled,
    string Tip);

public sealed record HairAccessoryLocGrpDatDocument(
    string Path,
    IReadOnlyList<HairAccessoryLocGrpDatEntry> Entries) : DatDocument(Path);

public sealed record HairAccessoryLocGrpDatEntry(
    string Name,
    IReadOnlyList<float[]> FloatTriples,
    IReadOnlyList<int[]> IntTriples);

public sealed record HennaGrpDatDocument(
    string Path,
    IReadOnlyList<HennaGrpDatEntry> Entries) : DatDocument(Path);

public sealed record HennaGrpDatEntry(
    uint Id,
    uint DyeId,
    string Name,
    string Icon,
    string SymbolAdditionalName,
    string SymbolAdditionalDescription);

public sealed record InstantZoneDataDatDocument(
    string Path,
    IReadOnlyList<InstantZoneDataDatEntry> Entries) : DatDocument(Path);

public sealed record InstantZoneDataDatEntry(
    uint Id,
    string Name);

public sealed record LogoGrpDatDocument(
    string Path,
    IReadOnlyList<LogoGrpDatEntry> Entries) : DatDocument(Path);

public sealed record LogoGrpDatEntry(
    int X,
    int Y,
    int Z,
    int Yaw);

public sealed record ObsceneDatDocument(
    string Path,
    IReadOnlyList<ObsceneDatEntry> Entries) : DatDocument(Path);

public sealed record ObsceneDatEntry(
    uint Id,
    string Text);

public sealed record OptionDataClientDatDocument(
    string Path,
    IReadOnlyList<OptionDataClientDatEntry> Entries) : DatDocument(Path);

public sealed record OptionDataClientDatEntry(
    uint Id,
    uint Level,
    uint LevelVariant,
    string Effect1Description,
    string Effect2Description,
    string Effect3Description);

public sealed record RaidDataDatDocument(
    string Path,
    IReadOnlyList<RaidDataDatEntry> Entries) : DatDocument(Path);

public sealed record RaidDataDatEntry(
    uint Id,
    uint NpcId,
    uint NpcLevel,
    uint AffiliatedAreaId,
    float LocationX,
    float LocationY,
    float LocationZ,
    string RaidDescription);

public sealed record ServerNameDatDocument(
    string Path,
    IReadOnlyList<ServerNameDatEntry> Entries) : DatDocument(Path);

public sealed record ServerNameDatEntry(
    uint ServerId,
    uint Tag,
    string ServerName,
    string ServerDescription);

public sealed record ShortcutAliasDatDocument(
    string Path,
    IReadOnlyList<ShortcutAliasDatEntry> Entries) : DatDocument(Path);

public sealed record ShortcutAliasDatEntry(
    uint Id,
    string Name,
    uint Value1,
    uint Value2);

public sealed record SkillSoundGrpDatDocument(
    string Path,
    IReadOnlyList<SkillSoundGrpDatEntry> Entries) : DatDocument(Path);

public sealed record SkillSoundGrpDatEntry(
    uint SkillId,
    uint SkillLevel,
    IReadOnlyList<string> SpellEffectSounds,
    IReadOnlyList<float> SpellEffectSoundValues,
    IReadOnlyList<string> ShotEffectSounds,
    IReadOnlyList<float> ShotEffectSoundValues,
    IReadOnlyList<string> ExpEffectSounds,
    IReadOnlyList<float> ExpEffectSoundValues,
    IReadOnlyList<string> CharacterSubSounds,
    IReadOnlyList<string> CharacterThrowSounds,
    float SoundVolume,
    float SoundRadius);

public sealed record SymbolNameDatDocument(
    string Path,
    IReadOnlyList<SymbolNameDatEntry> Entries) : DatDocument(Path);

public sealed record SymbolNameDatEntry(
    uint Id,
    string FileName,
    string Alias,
    uint Unknown0);

public sealed record SysStringDatDocument(
    string Path,
    IReadOnlyList<SysStringDatEntry> Entries) : DatDocument(Path);

public sealed record SysStringDatEntry(
    uint Id,
    string Name);

public sealed record TransformDataDatDocument(
    string Path,
    IReadOnlyList<TransformDataDatEntry> Entries) : DatDocument(Path);

public sealed record TransformDataDatEntry(
    uint Id,
    uint BinaryValue,
    uint Value1,
    uint Value2,
    string String1,
    string String2);

public sealed record RecipeDatDocument(
    string Path,
    IReadOnlyList<RecipeDatEntry> Entries) : DatDocument(Path);

public sealed record RecipeDatEntry(
    string Name,
    uint ManufactureItemId,
    uint RecipeId,
    uint Level,
    uint ResultItemId,
    uint ResultCount,
    uint MpCost,
    uint SuccessRate,
    IReadOnlyList<DatMaterialPair> Materials);

public sealed record DatMaterialPair(
    uint ItemId,
    uint Count);

public sealed record DatMeshTextureSet(
    IReadOnlyList<string> Meshes,
    IReadOnlyList<string> Textures);

public sealed record DatMeshVariantEntry(
    string Mesh,
    uint Value1,
    byte Value2);

public sealed record DatMeshTextureVariantSet(
    IReadOnlyList<string> Meshes,
    IReadOnlyList<DatMeshVariantEntry> Variants,
    IReadOnlyList<string> Textures);

public sealed record EtcItemGrpDatDocument(
    string Path,
    IReadOnlyList<EtcItemGrpDatEntry> Entries) : DatDocument(Path);

public sealed record EtcItemGrpDatEntry(
    uint Tag,
    uint Id,
    uint DropType,
    uint DropAnimationType,
    uint DropRadius,
    uint DropHeight,
    uint Unknown0,
    string DropMesh1,
    string DropMesh2,
    string DropMesh3,
    string DropTexture1,
    string DropTexture2,
    string DropTexture3,
    IReadOnlyList<string> Icons,
    uint Durability,
    uint Weight,
    uint Material,
    uint Crystallizable,
    uint Unknown1,
    IReadOnlyList<uint> Unknown2,
    string Fort,
    DatMeshTextureSet MeshTexturePair,
    string ItemSound,
    string EquipSound,
    uint Stackable,
    uint Family,
    uint Grade);

public sealed record WeaponGrpDatDocument(
    string Path,
    IReadOnlyList<WeaponGrpDatEntry> Entries) : DatDocument(Path);

public sealed record WeaponGrpDatEntry(
    uint Tag,
    uint Id,
    uint DropType,
    uint DropAnimationType,
    uint DropRadius,
    uint DropHeight,
    uint Unknown0,
    string DropMesh1,
    string DropMesh2,
    string DropMesh3,
    string DropTexture1,
    string DropTexture2,
    string DropTexture3,
    IReadOnlyList<string> Icons,
    int Durability,
    uint Weight,
    uint Material,
    uint Crystallizable,
    uint Unknown1,
    IReadOnlyList<uint> UnknownTable1,
    string TimeTab,
    uint BodyPart,
    uint Handness,
    IReadOnlyList<string> WeaponMeshes,
    IReadOnlyList<string> WeaponTextures,
    IReadOnlyList<string> ItemSounds,
    string DropSound,
    string EquipSound,
    string Effect,
    uint RandomDamage,
    uint PhysicalAttack,
    uint MagicalAttack,
    uint WeaponType,
    uint CrystalType,
    uint Critical,
    int HitModifier,
    int AvoidModifier,
    uint ShieldPhysicalDefense,
    uint ShieldRate,
    uint Speed,
    uint MpConsume,
    uint SoulShotCount,
    uint SpiritShotCount,
    uint Curvature,
    uint Unknown3,
    int IsHero,
    uint Unknown4,
    byte[] UnknownTail);

public sealed record ArmorGrpDatDocument(
    string Path,
    IReadOnlyList<ArmorGrpDatEntry> Entries) : DatDocument(Path);

public sealed record ArmorGrpDatEntry(
    uint Tag,
    uint Id,
    uint DropType,
    uint DropAnimationType,
    uint DropRadius,
    uint DropHeight,
    uint Unknown0,
    string DropMesh1,
    string DropMesh2,
    string DropMesh3,
    string DropTexture1,
    string DropTexture2,
    string DropTexture3,
    IReadOnlyList<string> Icons,
    uint Durability,
    uint Weight,
    uint Material,
    uint Crystallizable,
    uint Unknown1,
    IReadOnlyList<uint> Unknown2,
    string TimeTab,
    uint BodyPart,
    IReadOnlyList<ArmorMeshGroupDatEntry> MeshGroups,
    string AttackEffect,
    IReadOnlyList<string> ItemSounds,
    string DropSound,
    string EquipSound,
    uint Unknown3,
    uint Unknown4,
    uint ArmorType,
    uint CrystalType,
    uint AvoidModifier,
    uint PhysicalDefense,
    uint MagicalDefense,
    uint MpBonus,
    uint Unknown5);

public sealed record ArmorMeshGroupDatEntry(
    string Name,
    DatMeshTextureVariantSet Value);

public sealed record CharGrpEquipmentDatEntry(
    IReadOnlyList<string> Meshes,
    IReadOnlyList<string> Textures,
    IReadOnlyList<string> AdditionalMeshes,
    IReadOnlyList<string> AdditionalTextures,
    IReadOnlyList<byte> AdditionalBytes,
    IReadOnlyList<uint> AdditionalIntegers);

public sealed record CharGrpDatDocument(
    string Path,
    IReadOnlyList<CharGrpDatEntry> Entries) : DatDocument(Path);

public sealed record CharGrpDatEntry(
    IReadOnlyList<string> Hair,
    DatMeshTextureSet Face,
    byte[] ReservedBlock1,
    CharGrpEquipmentDatEntry Gloves,
    CharGrpEquipmentDatEntry Upper,
    CharGrpEquipmentDatEntry Lower,
    CharGrpEquipmentDatEntry Boots,
    byte[] ReservedBlock2,
    string AttackEffect,
    uint WalkAnimationFrame,
    IReadOnlyList<string> AttackSounds,
    IReadOnlyList<string> DefenseSounds,
    IReadOnlyList<string> DamageSounds,
    IReadOnlyList<string> VoiceHandSounds,
    IReadOnlyList<string> VoiceOneHandSwordSounds,
    IReadOnlyList<string> VoiceTwoHandSwordSounds,
    IReadOnlyList<string> VoiceDualSounds,
    IReadOnlyList<string> VoicePoleSounds,
    IReadOnlyList<string> Reserve1Sounds,
    IReadOnlyList<string> Reserve2Sounds,
    IReadOnlyList<string> Reserve3Sounds,
    IReadOnlyList<string> Reserve4Sounds,
    IReadOnlyList<string> Reserve5Sounds,
    uint FinalValue);
