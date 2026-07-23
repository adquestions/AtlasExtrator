using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AtlasExtractor
{
    internal static class GearDatasetExporter
    {
        public static int Export(string outputFolder)
        {
            string starUpPath = Path.Combine(
                outputFolder,
                "equipment_star_up.csv");

            string breakPath = Path.Combine(
                outputFolder,
                "equipment_break.csv");

            string equipmentPath = Path.Combine(
                outputFolder,
                "equipment_new.csv");

            string localizationPath = Path.Combine(
                outputFolder,
                "localization_en.csv");

            string suitGroupPath = Path.Combine(
                outputFolder,
                "equipment_suit_group.csv");

            RequireFile(starUpPath);
            RequireFile(breakPath);
            RequireFile(equipmentPath);
            RequireFile(localizationPath);
            RequireFile(suitGroupPath);

            List<Dictionary<string, string>> rows =
                ReadCsv(starUpPath);

            List<GearLevelCostRecord> records =
                rows
                    .Where(row =>
                        Get(row, "model_id") == "1511")
                    .Select(row =>
                        new GearLevelCostRecord
                        {
                            Level = ParseInt(
                                Get(row, "star")),

                            GearEnhanceRunes =
                                ParseItemAmount(
                                    Get(
                                        row,
                                        "star_up_cost"),
                                    "206")
                        })
                    .OrderBy(record => record.Level)
                    .ToList();

            string outputPath = Path.Combine(
                outputFolder,
                "current_gear_level_costs.csv");

            WriteLevelCosts(
                outputPath,
                records);

            List<GearBreakCostRecord> breakRecords =
                ReadCsv(breakPath)
                    .Where(row =>
                        Get(row, "model_id") == "1511" &&
                        ParseInt(Get(row, "lv")) >= 1)
                    .Select(ParseBreakCost)
                    .OrderBy(record => record.BreakLevel)
                    .ToList();

            string breakOutputPath = Path.Combine(
                outputFolder,
                "current_gear_break_costs.csv");

            WriteBreakCosts(
                breakOutputPath,
                breakRecords);

            List<GearUpgradeTotalRecord> totalRecords =
                BuildUpgradeTotals(
                    records,
                    breakRecords);

            string totalsOutputPath = Path.Combine(
                outputFolder,
                "current_gear_upgrade_totals.csv");

            WriteUpgradeTotals(
                totalsOutputPath,
                totalRecords);

            List<CurrentGearPieceRecord> pieceRecords =
                BuildCurrentGearPieces(
                    equipmentPath,
                    localizationPath);

            string piecesOutputPath = Path.Combine(
                outputFolder,
                "current_gear_pieces.csv");

            WriteCurrentGearPieces(
                piecesOutputPath,
                pieceRecords);

            List<CurrentGearSetBonusRecord> setBonusRecords =
                BuildCurrentGearSetBonuses(
                    suitGroupPath,
                    localizationPath);

            List<CurrentGearSetSummaryRecord> setSummaryRecords =
                BuildCurrentGearSetSummaries(
                    pieceRecords,
                    setBonusRecords);

            string setSummaryOutputPath = Path.Combine(
                outputFolder,
                "current_gear_set_summaries.csv");

            WriteCurrentGearSetSummaries(
                setSummaryOutputPath,
                setSummaryRecords);

            string setSummaryJsonOutputPath = Path.Combine(
                outputFolder,
                "current_gear_set_summaries.json");

            WriteCurrentGearSetSummariesJson(
                setSummaryJsonOutputPath,
                setSummaryRecords);

            string setBonusesOutputPath = Path.Combine(
                outputFolder,
                "current_gear_set_bonuses.csv");

            WriteCurrentGearSetBonuses(
                setBonusesOutputPath,
                setBonusRecords);

            Console.WriteLine();
            Console.WriteLine(
                "Current gear level costs");

            Console.WriteLine(
                "------------------------------");

            Console.WriteLine(
                "Rows exported: " +
                records.Count.ToString("N0"));

            Console.WriteLine(
                "Level CSV: " +
                outputPath);

            Console.WriteLine(
                "Break rows exported: " +
                breakRecords.Count.ToString("N0"));

            Console.WriteLine(
                "Break CSV: " +
                breakOutputPath);

            Console.WriteLine(
                "Totals rows exported: " +
                totalRecords.Count.ToString("N0"));

            Console.WriteLine(
                "Totals CSV: " +
                totalsOutputPath);

            Console.WriteLine(
                "Gear pieces exported: " +
                pieceRecords.Count.ToString("N0"));

            Console.WriteLine(
                "Gear pieces CSV: " +
                piecesOutputPath);

            Console.WriteLine(
                "Set summary rows exported: " +
                setSummaryRecords.Count.ToString("N0"));

            Console.WriteLine(
                "Set summaries CSV: " +
                setSummaryOutputPath);

            Console.WriteLine(
                "Set summaries JSON: " +
                setSummaryJsonOutputPath);

            Console.WriteLine(
                "Set bonus rows exported: " +
                setBonusRecords.Count.ToString("N0"));

            Console.WriteLine(
                "Set bonuses CSV: " +
                setBonusesOutputPath);

            return records.Count +
                   breakRecords.Count +
                   totalRecords.Count +
                   pieceRecords.Count +
                   setSummaryRecords.Count +
                   setBonusRecords.Count;
        }

        private static List<CurrentGearSetSummaryRecord>
            BuildCurrentGearSetSummaries(
                IEnumerable<CurrentGearPieceRecord> pieces,
                IEnumerable<CurrentGearSetBonusRecord> bonuses)
        {
            List<CurrentGearSetBonusRecord> bonusRecords =
                bonuses.ToList();

            return pieces
                .GroupBy(piece =>
                    new
                    {
                        piece.SuitId,
                        piece.SetName
                    })
                .Select(group =>
                    new CurrentGearSetSummaryRecord
                    {
                        SuitId = group.Key.SuitId,

                        SetName = group.Key.SetName,

                        BaseGearAttack =
                            group.Sum(piece =>
                                piece.GearAttack),

                        BaseGearHp =
                            group.Sum(piece =>
                                piece.GearHp),

                        BaseGearAttackPercent =
                            group.Sum(piece =>
                                piece.GearAttackPercent),

                        BaseGearHpPercent =
                            group.Sum(piece =>
                                piece.GearHpPercent),

                        Level40GearAttack =
                            group.Sum(piece =>
                                piece.Level40GearAttack),

                        Level40GearHp =
                            group.Sum(piece =>
                                piece.Level40GearHp),

                        Level40GearAttackPercent =
                            group.Sum(piece =>
                                piece.Level40GearAttackPercent),

                        Level40GearHpPercent =
                            group.Sum(piece =>
                                piece.Level40GearHpPercent),

                        Break25GearAttack =
                            group.Sum(piece =>
                                piece.Break25GearAttack),

                        Break25GearHp =
                            group.Sum(piece =>
                                piece.Break25GearHp),

                        Break25GearAttackPercent =
                            group.Sum(piece =>
                                piece.Break25GearAttackPercent),

                        Break25GearHpPercent =
                            group.Sum(piece =>
                                piece.Break25GearHpPercent),

                        BaseCritRate =
                            group.Where(piece => piece.SpecialAttributeId == 8)
                                .Sum(piece => piece.SpecialAttributeValue),

                        BaseCritDamage =
                            group.Where(piece => piece.SpecialAttributeId == 10)
                                .Sum(piece => piece.SpecialAttributeValue),

                        BaseBlock =
                            group.Where(piece => piece.SpecialAttributeId == 16)
                                .Sum(piece => piece.SpecialAttributeValue),

                        BaseNormalAttackDamageReduction =
                            group.Where(piece => piece.SpecialAttributeId == 26)
                                .Sum(piece => piece.SpecialAttributeValue),

                        Level40CritRate =
                            group.Where(piece => piece.SpecialAttributeId == 8)
                                .Sum(piece => piece.Level40SpecialAttributeValue),

                        Level40CritDamage =
                            group.Where(piece => piece.SpecialAttributeId == 10)
                                .Sum(piece => piece.Level40SpecialAttributeValue),

                        Level40Block =
                            group.Where(piece => piece.SpecialAttributeId == 16)
                                .Sum(piece => piece.Level40SpecialAttributeValue),

                        Level40NormalAttackDamageReduction =
                            group.Where(piece => piece.SpecialAttributeId == 26)
                                .Sum(piece => piece.Level40SpecialAttributeValue),

                        Break25CritRate =
                            group.Where(piece => piece.SpecialAttributeId == 8)
                                .Sum(piece => piece.Break25SpecialAttributeValue),

                        Break25CritDamage =
                            group.Where(piece => piece.SpecialAttributeId == 10)
                                .Sum(piece => piece.Break25SpecialAttributeValue),

                        Break25Block =
                            group.Where(piece => piece.SpecialAttributeId == 16)
                                .Sum(piece => piece.Break25SpecialAttributeValue),

                        Break25NormalAttackDamageReduction =
                            group.Where(piece => piece.SpecialAttributeId == 26)
                                .Sum(piece => piece.Break25SpecialAttributeValue),

                        BaseSetBonusDescription =
                            bonusRecords
                                .Where(bonus =>
                                    bonus.SuitId == group.Key.SuitId &&
                                    bonus.Tier == 1)
                                .Select(bonus => bonus.Description)
                                .FirstOrDefault(),

                        Level40SetBonusDescription =
                            bonusRecords
                                .Where(bonus =>
                                    bonus.SuitId == group.Key.SuitId &&
                                    bonus.Tier == 2)
                                .Select(bonus => bonus.Description)
                                .FirstOrDefault(),

                        MythicSetBonusDescription =
                            bonusRecords
                                .Where(bonus =>
                                    bonus.SuitId == group.Key.SuitId &&
                                    bonus.Tier == 3)
                                .Select(bonus => bonus.Description)
                                .FirstOrDefault(),

                        Level40SetBonusRequiredGearLevel =
                            bonusRecords
                                .Where(bonus =>
                                    bonus.SuitId == group.Key.SuitId &&
                                    bonus.Tier == 2)
                                .Select(bonus => bonus.RequiredGearLevel)
                                .FirstOrDefault(),

                        MythicSetBonusRequiredBreakLevel =
                            bonusRecords
                                .Where(bonus =>
                                    bonus.SuitId == group.Key.SuitId &&
                                    bonus.Tier == 3)
                                .Select(bonus => bonus.RequiredBreakLevel)
                                .FirstOrDefault()
                    })
                .OrderBy(record =>
                    record.SuitId)
                .ToList();
        }
        private static void WriteCurrentGearSetSummaries(
            string outputPath,
            IEnumerable<CurrentGearSetSummaryRecord> records)
        {
            var encoding =
                new UTF8Encoding(true);

            using (var writer = new StreamWriter(
                outputPath,
                false,
                encoding))
            {
                writer.WriteLine(
                    "SuitId," +
                    "SetName," +
                    "BaseGearAttack," +
                    "BaseGearHp," +
                    "BaseGearAttackPercent," +
                    "BaseGearHpPercent," +
                    "Level40GearAttack," +
                    "Level40GearHp," +
                    "Level40GearAttackPercent," +
                    "Level40GearHpPercent," +
                    "Break25GearAttack," +
                    "Break25GearHp," +
                    "Break25GearAttackPercent," +
                    "Break25GearHpPercent," +
                    "BaseCritRate," +
                    "BaseCritDamage," +
                    "BaseBlock," +
                    "BaseNormalAttackDamageReduction," +
                    "Level40CritRate," +
                    "Level40CritDamage," +
                    "Level40Block," +
                    "Level40NormalAttackDamageReduction," +
                    "Break25CritRate," +
                    "Break25CritDamage," +
                    "Break25Block," +
                    "Break25NormalAttackDamageReduction," +
                    "BaseSetBonusDescription," +
                    "Level40SetBonusDescription," +
                    "MythicSetBonusDescription," +
                    "Level40SetBonusRequiredGearLevel," +
                    "MythicSetBonusRequiredBreakLevel");

                foreach (CurrentGearSetSummaryRecord record
                    in records)
                {
                    writer.WriteLine(
                        record.SuitId.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        EscapeCsv(record.SetName) +
                        "," +
                        record.BaseGearAttack.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.BaseGearHp.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.BaseGearAttackPercent.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.BaseGearHpPercent.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Level40GearAttack.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Level40GearHp.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Level40GearAttackPercent.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Level40GearHpPercent.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Break25GearAttack.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Break25GearHp.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Break25GearAttackPercent.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Break25GearHpPercent.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.BaseCritRate.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.BaseCritDamage.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.BaseBlock.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.BaseNormalAttackDamageReduction.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Level40CritRate.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Level40CritDamage.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Level40Block.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Level40NormalAttackDamageReduction.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Break25CritRate.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Break25CritDamage.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Break25Block.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Break25NormalAttackDamageReduction.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        EscapeCsv(record.BaseSetBonusDescription) +
                        "," +
                        EscapeCsv(record.Level40SetBonusDescription) +
                        "," +
                        EscapeCsv(record.MythicSetBonusDescription) +
                        "," +
                        record.Level40SetBonusRequiredGearLevel.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.MythicSetBonusRequiredBreakLevel.ToString(
                            CultureInfo.InvariantCulture));
                }
            }
        }
        private static void WriteCurrentGearSetSummariesJson(
            string outputPath,
            IEnumerable<CurrentGearSetSummaryRecord> records)
        {
            var encoding =
                new UTF8Encoding(true);

            List<CurrentGearSetSummaryRecord> recordList =
                records.ToList();

            using (var writer = new StreamWriter(
                outputPath,
                false,
                encoding))
            {
                writer.WriteLine("[");

                for (int index = 0;
                    index < recordList.Count;
                    index++)
                {
                    CurrentGearSetSummaryRecord record =
                        recordList[index];

                    writer.WriteLine("  {");

                    WriteJsonNumberProperty(writer, "suitId", record.SuitId, true);
                    WriteJsonStringProperty(writer, "setName", record.SetName, true);
                    WriteJsonNumberProperty(writer, "baseGearAttack", record.BaseGearAttack, true);
                    WriteJsonNumberProperty(writer, "baseGearHp", record.BaseGearHp, true);
                    WriteJsonNumberProperty(writer, "baseGearAttackPercent", record.BaseGearAttackPercent, true);
                    WriteJsonNumberProperty(writer, "baseGearHpPercent", record.BaseGearHpPercent, true);
                    WriteJsonNumberProperty(writer, "level40GearAttack", record.Level40GearAttack, true);
                    WriteJsonNumberProperty(writer, "level40GearHp", record.Level40GearHp, true);
                    WriteJsonNumberProperty(writer, "level40GearAttackPercent", record.Level40GearAttackPercent, true);
                    WriteJsonNumberProperty(writer, "level40GearHpPercent", record.Level40GearHpPercent, true);
                    WriteJsonNumberProperty(writer, "break25GearAttack", record.Break25GearAttack, true);
                    WriteJsonNumberProperty(writer, "break25GearHp", record.Break25GearHp, true);
                    WriteJsonNumberProperty(writer, "break25GearAttackPercent", record.Break25GearAttackPercent, true);
                    WriteJsonNumberProperty(writer, "break25GearHpPercent", record.Break25GearHpPercent, true);
                    WriteJsonNumberProperty(writer, "baseCritRate", record.BaseCritRate, true);
                    WriteJsonNumberProperty(writer, "baseCritDamage", record.BaseCritDamage, true);
                    WriteJsonNumberProperty(writer, "baseBlock", record.BaseBlock, true);
                    WriteJsonNumberProperty(writer, "baseNormalAttackDamageReduction", record.BaseNormalAttackDamageReduction, true);
                    WriteJsonNumberProperty(writer, "level40CritRate", record.Level40CritRate, true);
                    WriteJsonNumberProperty(writer, "level40CritDamage", record.Level40CritDamage, true);
                    WriteJsonNumberProperty(writer, "level40Block", record.Level40Block, true);
                    WriteJsonNumberProperty(writer, "level40NormalAttackDamageReduction", record.Level40NormalAttackDamageReduction, true);
                    WriteJsonNumberProperty(writer, "break25CritRate", record.Break25CritRate, true);
                    WriteJsonNumberProperty(writer, "break25CritDamage", record.Break25CritDamage, true);
                    WriteJsonNumberProperty(writer, "break25Block", record.Break25Block, true);
                    WriteJsonNumberProperty(writer, "break25NormalAttackDamageReduction", record.Break25NormalAttackDamageReduction, true);
                    WriteJsonStringProperty(writer, "baseSetBonusDescription", record.BaseSetBonusDescription, true);
                    WriteJsonStringProperty(writer, "level40SetBonusDescription", record.Level40SetBonusDescription, true);
                    WriteJsonStringProperty(writer, "mythicSetBonusDescription", record.MythicSetBonusDescription, true);
                    WriteJsonNumberProperty(writer, "level40SetBonusRequiredGearLevel", record.Level40SetBonusRequiredGearLevel, true);
                    WriteJsonNumberProperty(writer, "mythicSetBonusRequiredBreakLevel", record.MythicSetBonusRequiredBreakLevel, false);

                    writer.Write("  }");

                    if (index < recordList.Count - 1)
                    {
                        writer.Write(",");
                    }

                    writer.WriteLine();
                }

                writer.WriteLine("]");
            }
        }

        private static List<CurrentGearSetBonusRecord>
            BuildCurrentGearSetBonuses(
                string suitGroupPath,
                string localizationPath)
        {
            Dictionary<string, string> localization =
                ReadCsv(localizationPath)
                    .Where(row =>
                        !string.IsNullOrWhiteSpace(
                            Get(row, "key")))
                    .GroupBy(row =>
                        Get(row, "key"))
                    .ToDictionary(
                        group => group.Key,
                        group => Get(
                            group.First(),
                            "value"),
                        StringComparer.OrdinalIgnoreCase);

            var setNames =
                new Dictionary<string, string>
                {
                    { "1101", "Titan's Might" },
                    { "1102", "Fury of Blood" },
                    { "1103", "Glory of the Knight" }
                };

            return ReadCsv(suitGroupPath)
                .Where(row =>
                    setNames.ContainsKey(
                        Get(row, "group_id")))
                .Select(row =>
                {
                    string suitId =
                        Get(row, "group_id");

                    int tier =
                        ParseInt(
                            Get(row, "Priority"));

                    string descriptionKey =
                        "equipment_suit_desc_" +
                        suitId +
                        "_" +
                        tier.ToString(
                            CultureInfo.InvariantCulture);

                    string description;

                    if (!localization.TryGetValue(
                        descriptionKey,
                        out description))
                    {
                        description = descriptionKey;
                    }

                    return new CurrentGearSetBonusRecord
                    {
                        SuitId = ParseInt(suitId),

                        SetName = setNames[suitId],

                        Tier = tier,

                        RequiredGearLevel =
                            ParseInt(
                                Get(row, "star_up")),

                        RequiredBreakLevel =
                            ParseInt(
                                Get(row, "break_lv")),

                        Description = description
                    };
                })
                .OrderBy(record => record.SuitId)
                .ThenBy(record => record.Tier)
                .ToList();
        }
        private static void WriteCurrentGearSetBonuses(
            string outputPath,
            IEnumerable<CurrentGearSetBonusRecord> records)
        {
            var encoding =
                new UTF8Encoding(true);

            using (var writer = new StreamWriter(
                outputPath,
                false,
                encoding))
            {
                writer.WriteLine(
                    "SuitId," +
                    "SetName," +
                    "Tier," +
                    "RequiredGearLevel," +
                    "RequiredBreakLevel," +
                    "Description");

                foreach (CurrentGearSetBonusRecord record
                    in records)
                {
                    writer.WriteLine(
                        record.SuitId.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        EscapeCsv(record.SetName) +
                        "," +
                        record.Tier.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.RequiredGearLevel.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.RequiredBreakLevel.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        EscapeCsv(record.Description));
                }
            }
        }
        private static List<CurrentGearPieceRecord>
            BuildCurrentGearPieces(
                string equipmentPath,
                string localizationPath)
        {
            Dictionary<string, string> localization =
                ReadCsv(localizationPath)
                    .Where(row =>
                        !string.IsNullOrWhiteSpace(
                            Get(row, "key")))
                    .GroupBy(row =>
                        Get(row, "key"))
                    .ToDictionary(
                        group => group.Key,
                        group => Get(
                            group.First(),
                            "value"),
                        StringComparer.OrdinalIgnoreCase);

            var setNames =
                new Dictionary<string, string>
                {
                    { "1101", "Titan's Might" },
                    { "1102", "Fury of Blood" },
                    { "1103", "Glory of the Knight" }
                };

            var slotNames =
                new Dictionary<string, string>
                {
                    { "1", "Weapon" },
                    { "2", "Armor" },
                    { "3", "Boots" },
                    { "4", "Helm" }
                };

            Dictionary<string, Dictionary<string, string>>
                level40Rows =
                    ReadCsv(
                        Path.Combine(
                            Path.GetDirectoryName(
                                equipmentPath),
                            "equipment_star_up.csv"))
                        .Where(row =>
                            Get(row, "star") == "40")
                        .GroupBy(row =>
                            Get(row, "model_id"))
                        .ToDictionary(
                            group => group.Key,
                            group => group.First(),
                            StringComparer.OrdinalIgnoreCase);

            Dictionary<string, Dictionary<string, string>>
                break25Rows =
                    ReadCsv(
                        Path.Combine(
                            Path.GetDirectoryName(
                                equipmentPath),
                            "equipment_break.csv"))
                        .Where(row =>
                            Get(row, "lv") == "25")
                        .GroupBy(row =>
                            Get(row, "model_id"))
                        .ToDictionary(
                            group => group.Key,
                            group => group.First(),
                            StringComparer.OrdinalIgnoreCase);

            return ReadCsv(equipmentPath)
                .Where(row =>
                    setNames.ContainsKey(
                        Get(row, "suit_id")))
                .Select(row =>
                {
                    string nameKey =
                        Get(row, "name");

                    string suitId =
                        Get(row, "suit_id");

                    string type =
                        Get(row, "type");

                    string localizedName;

                    if (!localization.TryGetValue(
                        nameKey,
                        out localizedName))
                    {
                        localizedName = nameKey;
                    }

                    Dictionary<int, int> attributes =
                        ParseAttributes(
                            Get(row, "attr"));

                    Dictionary<string, string> level40Row;
                    Dictionary<int, int> level40Attributes =
                        level40Rows.TryGetValue(
                            Get(row, "id"),
                            out level40Row)
                            ? ParseAttributes(
                                Get(
                                    level40Row,
                                    "attribute"))
                            : new Dictionary<int, int>();

                    Dictionary<string, string> break25Row;
                    Dictionary<int, int> break25Attributes =
                        break25Rows.TryGetValue(
                            Get(row, "id"),
                            out break25Row)
                            ? ParseAttributes(
                                Get(
                                    break25Row,
                                    "attribute"))
                            : new Dictionary<int, int>();

                    int specialAttributeId =
                        attributes.Keys
                            .Where(id =>
                                id != 65 &&
                                id != 66 &&
                                id != 165 &&
                                id != 166)
                            .FirstOrDefault();

                    var specialAttributeNames =
                        new Dictionary<int, string>
                        {
                            { 8, "Crit Rate" },
                            { 10, "Crit Damage" },
                            { 16, "Block" },
                            { 26, "Normal ATK DMG RED" }
                        };

                    return new CurrentGearPieceRecord
                    {
                        Id = ParseInt(
                            Get(row, "id")),

                        SuitId = ParseInt(suitId),

                        Type = ParseInt(type),

                        Quality = ParseInt(
                            Get(row, "quality")),

                        Name = localizedName,

                        SetName = setNames[suitId],

                        SlotName = slotNames.ContainsKey(type)
                            ? slotNames[type]
                            : type,

                        GearAttack =
                            attributes.ContainsKey(65)
                                ? attributes[65]
                                : 0,

                        GearHp =
                            attributes.ContainsKey(66)
                                ? attributes[66]
                                : 0,

                        GearAttackPercent =
                            attributes.ContainsKey(165)
                                ? attributes[165]
                                : 0,

                        GearHpPercent =
                            attributes.ContainsKey(166)
                                ? attributes[166]
                                : 0,

                        SpecialAttributeId =
                            specialAttributeId,

                        SpecialAttributeValue =
                            specialAttributeId != 0 &&
                            attributes.ContainsKey(
                                specialAttributeId)
                                ? attributes[
                                    specialAttributeId]
                                : 0,

                        SpecialAttributeName =
                            specialAttributeNames.ContainsKey(
                                specialAttributeId)
                                ? specialAttributeNames[
                                    specialAttributeId]
                                : string.Empty,

                        Level40GearAttack =
                            level40Attributes.ContainsKey(65)
                                ? level40Attributes[65]
                                : 0,

                        Level40GearHp =
                            level40Attributes.ContainsKey(66)
                                ? level40Attributes[66]
                                : 0,

                        Level40GearAttackPercent =
                            level40Attributes.ContainsKey(165)
                                ? level40Attributes[165]
                                : 0,

                        Level40GearHpPercent =
                            level40Attributes.ContainsKey(166)
                                ? level40Attributes[166]
                                : 0,

                        Level40SpecialAttributeValue =
                            specialAttributeId != 0 &&
                            level40Attributes.ContainsKey(
                                specialAttributeId)
                                ? level40Attributes[
                                    specialAttributeId]
                                : 0,

                        Break25GearAttack =
                            break25Attributes.ContainsKey(65)
                                ? break25Attributes[65]
                                : 0,

                        Break25GearHp =
                            break25Attributes.ContainsKey(66)
                                ? break25Attributes[66]
                                : 0,

                        Break25GearAttackPercent =
                            break25Attributes.ContainsKey(165)
                                ? break25Attributes[165]
                                : 0,

                        Break25GearHpPercent =
                            break25Attributes.ContainsKey(166)
                                ? break25Attributes[166]
                                : 0,

                        Break25SpecialAttributeValue =
                            specialAttributeId != 0 &&
                            break25Attributes.ContainsKey(
                                specialAttributeId)
                                ? break25Attributes[
                                    specialAttributeId]
                                : 0,

                        MythicGearAttack =
                            (level40Attributes.ContainsKey(65)
                                ? level40Attributes[65]
                                : 0) +
                            (break25Attributes.ContainsKey(65)
                                ? break25Attributes[65]
                                : 0),

                        MythicGearHp =
                            (level40Attributes.ContainsKey(66)
                                ? level40Attributes[66]
                                : 0) +
                            (break25Attributes.ContainsKey(66)
                                ? break25Attributes[66]
                                : 0),

                        MythicGearAttackPercent =
                            (level40Attributes.ContainsKey(165)
                                ? level40Attributes[165]
                                : 0) +
                            (break25Attributes.ContainsKey(165)
                                ? break25Attributes[165]
                                : 0),

                        MythicGearHpPercent =
                            (level40Attributes.ContainsKey(166)
                                ? level40Attributes[166]
                                : 0) +
                            (break25Attributes.ContainsKey(166)
                                ? break25Attributes[166]
                                : 0),

                        MythicSpecialAttributeValue =
                            (specialAttributeId != 0 &&
                             level40Attributes.ContainsKey(
                                 specialAttributeId)
                                ? level40Attributes[
                                    specialAttributeId]
                                : 0) +
                            (specialAttributeId != 0 &&
                             break25Attributes.ContainsKey(
                                 specialAttributeId)
                                ? break25Attributes[
                                    specialAttributeId]
                                : 0),

                        BaseAttributes =
                            Get(row, "attr"),

                        BasePower =
                            Get(row, "basePowers")
                    };
                })
                .OrderBy(record => record.SuitId)
                .ThenBy(record => record.Type)
                .ToList();
        }
        private static void WriteCurrentGearPieces(
            string outputPath,
            IEnumerable<CurrentGearPieceRecord> records)
        {
            var encoding =
                new UTF8Encoding(true);

            using (var writer = new StreamWriter(
                outputPath,
                false,
                encoding))
            {
                writer.WriteLine(
                    "Id," +
                    "SuitId," +
                    "SetName," +
                    "Type," +
                    "SlotName," +
                    "Quality," +
                    "Name," +
                    "GearAttack," +
                    "GearHp," +
                    "GearAttackPercent," +
                    "GearHpPercent," +
                    "SpecialAttributeId," +
                    "SpecialAttributeName," +
                    "SpecialAttributeValue," +
                    "Level40GearAttack," +
                    "Level40GearHp," +
                    "Level40GearAttackPercent," +
                    "Level40GearHpPercent," +
                    "Level40SpecialAttributeValue," +
                    "Break25GearAttack," +
                    "Break25GearHp," +
                    "Break25GearAttackPercent," +
                    "Break25GearHpPercent," +
                    "Break25SpecialAttributeValue," +
                    "MythicGearAttack," +
                    "MythicGearHp," +
                    "MythicGearAttackPercent," +
                    "MythicGearHpPercent," +
                    "MythicSpecialAttributeValue," +
                    "BaseAttributes," +
                    "BasePower");

                foreach (CurrentGearPieceRecord record
                    in records)
                {
                    writer.WriteLine(
                        record.Id.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.SuitId.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        EscapeCsv(record.SetName) +
                        "," +
                        record.Type.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        EscapeCsv(record.SlotName) +
                        "," +
                        record.Quality.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        EscapeCsv(record.Name) +
                        "," +
                        record.GearAttack.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.GearHp.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.GearAttackPercent.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.GearHpPercent.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.SpecialAttributeId.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        EscapeCsv(
                            record.SpecialAttributeName) +
                        "," +
                        record.SpecialAttributeValue.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Level40GearAttack.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Level40GearHp.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Level40GearAttackPercent.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Level40GearHpPercent.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Level40SpecialAttributeValue.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Break25GearAttack.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Break25GearHp.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Break25GearAttackPercent.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Break25GearHpPercent.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Break25SpecialAttributeValue.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.MythicGearAttack.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.MythicGearHp.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.MythicGearAttackPercent.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.MythicGearHpPercent.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.MythicSpecialAttributeValue.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        EscapeCsv(record.BaseAttributes) +
                        "," +
                        EscapeCsv(record.BasePower));
                }
            }
        }
        private static List<GearUpgradeTotalRecord>
            BuildUpgradeTotals(
                IEnumerable<GearLevelCostRecord> levelRecords,
                IEnumerable<GearBreakCostRecord> breakRecords)
        {
            int levelRunes =
                levelRecords.Sum(
                    record =>
                        record.GearEnhanceRunes);

            int breakRunes =
                breakRecords.Sum(
                    record =>
                        record.GearEnhanceRunes);

            int ingots =
                breakRecords.Sum(
                    record =>
                        record.SolarGoldIngots);

            int rubies =
                breakRecords.Sum(
                    record =>
                        record.Rubies);

            int legendaryStones =
                breakRecords.Sum(
                    record =>
                        record.LegendaryPromotionStones);

            int mythicStones =
                breakRecords.Sum(
                    record =>
                        record.MythicPromotionStones);

            var onePiece =
                new GearUpgradeTotalRecord
                {
                    Scope = "One Piece",
                    GearEnhanceRunes =
                        levelRunes + breakRunes,
                    SolarGoldIngots = ingots,
                    Rubies = rubies,
                    LegendaryPromotionStones =
                        legendaryStones,
                    MythicPromotionStones =
                        mythicStones
                };

            var fourPieceSet =
                new GearUpgradeTotalRecord
                {
                    Scope = "Four-Piece Set",
                    GearEnhanceRunes =
                        onePiece.GearEnhanceRunes * 4,
                    SolarGoldIngots =
                        onePiece.SolarGoldIngots * 4,
                    Rubies =
                        onePiece.Rubies * 4,
                    LegendaryPromotionStones =
                        onePiece.LegendaryPromotionStones * 4,
                    MythicPromotionStones =
                        onePiece.MythicPromotionStones * 4
                };

            return new List<GearUpgradeTotalRecord>
            {
                onePiece,
                fourPieceSet
            };
        }

        private static void WriteUpgradeTotals(
            string outputPath,
            IEnumerable<GearUpgradeTotalRecord> records)
        {
            var encoding =
                new UTF8Encoding(true);

            using (var writer = new StreamWriter(
                outputPath,
                false,
                encoding))
            {
                writer.WriteLine(
                    "Scope," +
                    "GearEnhanceRunes," +
                    "SolarGoldIngots," +
                    "Rubies," +
                    "LegendaryPromotionStones," +
                    "MythicPromotionStones");

                foreach (GearUpgradeTotalRecord record
                    in records)
                {
                    writer.WriteLine(
                        EscapeCsv(record.Scope) +
                        "," +
                        record.GearEnhanceRunes.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.SolarGoldIngots.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Rubies.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.LegendaryPromotionStones.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.MythicPromotionStones.ToString(
                            CultureInfo.InvariantCulture));
                }
            }
        }
        private static GearBreakCostRecord ParseBreakCost(
            Dictionary<string, string> row)
        {
            var record = new GearBreakCostRecord
            {
                BreakLevel =
                    ParseInt(Get(row, "lv"))
            };

            string costs =
                Get(row, "break_cost_goods") +
                "|" +
                Get(row, "break_cost_no_back");

            foreach (Match match in Regex.Matches(
                costs,
                @"(\d+)\|(\d+)\|(\d+)"))
            {
                string type =
                    match.Groups[1].Value;

                string id =
                    match.Groups[2].Value;

                int amount =
                    ParseInt(match.Groups[3].Value);

                string key =
                    type + "|" + id;

                switch (key)
                {
                    case "2|404100005":
                        record.SolarGoldIngots += amount;
                        break;

                    case "2|206":
                        record.GearEnhanceRunes += amount;
                        break;

                    case "999|6":
                        record.Rubies += amount;
                        break;

                    case "2|151":
                        record.LegendaryPromotionStones +=
                            amount;
                        break;

                    case "2|152":
                        record.MythicPromotionStones +=
                            amount;
                        break;
                }
            }

            return record;
        }

        private static void WriteBreakCosts(
            string outputPath,
            IEnumerable<GearBreakCostRecord> records)
        {
            var encoding =
                new UTF8Encoding(true);

            using (var writer = new StreamWriter(
                outputPath,
                false,
                encoding))
            {
                writer.WriteLine(
                    "BreakLevel," +
                    "SolarGoldIngots," +
                    "GearEnhanceRunes," +
                    "Rubies," +
                    "LegendaryPromotionStones," +
                    "MythicPromotionStones");

                foreach (GearBreakCostRecord record
                    in records)
                {
                    writer.WriteLine(
                        record.BreakLevel.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.SolarGoldIngots.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.GearEnhanceRunes.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.Rubies.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.LegendaryPromotionStones.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.MythicPromotionStones.ToString(
                            CultureInfo.InvariantCulture));
                }
            }
        }
        private static Dictionary<int, int>
            ParseAttributes(string value)
        {
            var attributes =
                new Dictionary<int, int>();

            if (string.IsNullOrWhiteSpace(value))
            {
                return attributes;
            }

            MatchCollection matches =
                Regex.Matches(
                    value,
                    @"(\d+)\|(-?\d+)");

            foreach (Match match in matches)
            {
                int attributeId =
                    ParseInt(match.Groups[1].Value);

                int amount =
                    ParseInt(match.Groups[2].Value);

                attributes[attributeId] = amount;
            }

            return attributes;
        }
        private static int ParseItemAmount(
            string value,
            string itemId)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            Match match = Regex.Match(
                value,
                @"\[2\|" +
                Regex.Escape(itemId) +
                @"\|(\d+)\]");

            return match.Success
                ? ParseInt(match.Groups[1].Value)
                : 0;
        }

        private static void WriteLevelCosts(
            string outputPath,
            IEnumerable<GearLevelCostRecord> records)
        {
            var encoding =
                new UTF8Encoding(true);

            using (var writer = new StreamWriter(
                outputPath,
                false,
                encoding))
            {
                writer.WriteLine(
                    "Level,GearEnhanceRunes");

                foreach (GearLevelCostRecord record
                    in records)
                {
                    writer.WriteLine(
                        record.Level.ToString(
                            CultureInfo.InvariantCulture) +
                        "," +
                        record.GearEnhanceRunes.ToString(
                            CultureInfo.InvariantCulture));
                }
            }
        }

        private static List<Dictionary<string, string>>
            ReadCsv(string path)
        {
            var rows =
                new List<List<string>>();

            var currentRow =
                new List<string>();

            var currentField =
                new StringBuilder();

            bool insideQuotes = false;

            using (var reader = new StreamReader(
                path,
                Encoding.UTF8,
                true))
            {
                while (true)
                {
                    int next = reader.Read();

                    if (next < 0)
                    {
                        if (insideQuotes)
                        {
                            throw new InvalidDataException(
                                "CSV ended inside a quoted field: " +
                                path);
                        }

                        if (currentField.Length > 0 ||
                            currentRow.Count > 0)
                        {
                            currentRow.Add(
                                currentField.ToString());

                            rows.Add(currentRow);
                        }

                        break;
                    }

                    char character = (char)next;

                    if (insideQuotes)
                    {
                        if (character == '"')
                        {
                            if (reader.Peek() == '"')
                            {
                                reader.Read();
                                currentField.Append('"');
                            }
                            else
                            {
                                insideQuotes = false;
                            }
                        }
                        else
                        {
                            currentField.Append(character);
                        }

                        continue;
                    }

                    if (character == '"' &&
                        currentField.Length == 0)
                    {
                        insideQuotes = true;
                    }
                    else if (character == ',')
                    {
                        currentRow.Add(
                            currentField.ToString());

                        currentField.Clear();
                    }
                    else if (character == '\r')
                    {
                        if (reader.Peek() == '\n')
                        {
                            reader.Read();
                        }

                        currentRow.Add(
                            currentField.ToString());

                        rows.Add(currentRow);

                        currentRow =
                            new List<string>();

                        currentField.Clear();
                    }
                    else if (character == '\n')
                    {
                        currentRow.Add(
                            currentField.ToString());

                        rows.Add(currentRow);

                        currentRow =
                            new List<string>();

                        currentField.Clear();
                    }
                    else
                    {
                        currentField.Append(character);
                    }
                }
            }

            if (rows.Count == 0)
            {
                return new List<
                    Dictionary<string, string>>();
            }

            List<string> headers =
                rows[0];

            var result =
                new List<
                    Dictionary<string, string>>();

            for (int rowIndex = 1;
                 rowIndex < rows.Count;
                 rowIndex++)
            {
                List<string> values =
                    rows[rowIndex];

                if (values.Count == 1 &&
                    values[0].Length == 0)
                {
                    continue;
                }

                var dictionary =
                    new Dictionary<string, string>(
                        StringComparer.OrdinalIgnoreCase);

                for (int columnIndex = 0;
                     columnIndex < headers.Count;
                     columnIndex++)
                {
                    dictionary[headers[columnIndex]] =
                        columnIndex < values.Count
                            ? values[columnIndex]
                            : string.Empty;
                }

                result.Add(dictionary);
            }

            return result;
        }

        private static string Get(
            IDictionary<string, string> row,
            string column)
        {
            if (row == null)
            {
                return string.Empty;
            }

            string value;

            return row.TryGetValue(
                    column,
                    out value)
                ? value ?? string.Empty
                : string.Empty;
        }

        private static string EscapeCsv(string value)
        {
            value = value ?? string.Empty;

            bool requiresQuotes =
                value.IndexOf(',') >= 0 ||
                value.IndexOf('"') >= 0 ||
                value.IndexOf('\r') >= 0 ||
                value.IndexOf('\n') >= 0;

            if (!requiresQuotes)
            {
                return value;
            }

            return "\"" +
                   value.Replace("\"", "\"\"") +
                   "\"";
        }
        private static int ParseInt(string value)
        {
            int result;

            return int.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out result)
                ? result
                : 0;
        }

        private static void RequireFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "Required dataset file was not found.",
                    path);
            }
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var result =
                new StringBuilder();

            foreach (char character in value)
            {
                switch (character)
                {
                    case '\\':
                        result.Append("\\\\");
                        break;

                    case '"':
                        result.Append("\\\"");
                        break;

                    case '\r':
                        result.Append("\\r");
                        break;

                    case '\n':
                        result.Append("\\n");
                        break;

                    case '\t':
                        result.Append("\\t");
                        break;

                    default:
                        if (character < 32)
                        {
                            result.Append(
                                "\\u" +
                                ((int)character).ToString(
                                    "x4"));
                        }
                        else
                        {
                            result.Append(character);
                        }

                        break;
                }
            }

            return result.ToString();
        }

        private static void WriteJsonStringProperty(
            StreamWriter writer,
            string name,
            string value,
            bool appendComma)
        {
            writer.Write("      \"");
            writer.Write(EscapeJson(name));
            writer.Write("\": \"");
            writer.Write(EscapeJson(value));
            writer.Write("\"");

            if (appendComma)
            {
                writer.Write(",");
            }

            writer.WriteLine();
        }

        private static void WriteJsonNumberProperty(
            StreamWriter writer,
            string name,
            int value,
            bool appendComma)
        {
            writer.Write("      \"");
            writer.Write(EscapeJson(name));
            writer.Write("\": ");
            writer.Write(
                value.ToString(
                    CultureInfo.InvariantCulture));

            if (appendComma)
            {
                writer.Write(",");
            }

            writer.WriteLine();
        }

        private sealed class CurrentGearSetSummaryRecord
        {
            public int SuitId { get; set; }

            public string SetName { get; set; }

            public int BaseGearAttack { get; set; }

            public int BaseGearHp { get; set; }

            public int BaseGearAttackPercent { get; set; }

            public int BaseGearHpPercent { get; set; }

            public int Level40GearAttack { get; set; }

            public int Level40GearHp { get; set; }

            public int Level40GearAttackPercent { get; set; }

            public int Level40GearHpPercent { get; set; }

            public int Break25GearAttack { get; set; }

            public int Break25GearHp { get; set; }

            public int Break25GearAttackPercent { get; set; }

            public int Break25GearHpPercent { get; set; }

            public int BaseCritRate { get; set; }

            public int BaseCritDamage { get; set; }

            public int BaseBlock { get; set; }

            public int BaseNormalAttackDamageReduction { get; set; }

            public int Level40CritRate { get; set; }

            public int Level40CritDamage { get; set; }

            public int Level40Block { get; set; }

            public int Level40NormalAttackDamageReduction { get; set; }

            public int Break25CritRate { get; set; }

            public int Break25CritDamage { get; set; }

            public int Break25Block { get; set; }

            public int Break25NormalAttackDamageReduction { get; set; }

            public string BaseSetBonusDescription { get; set; }

            public string Level40SetBonusDescription { get; set; }

            public string MythicSetBonusDescription { get; set; }

            public int Level40SetBonusRequiredGearLevel { get; set; }

            public int MythicSetBonusRequiredBreakLevel { get; set; }
        }
        private sealed class CurrentGearSetBonusRecord
        {
            public int SuitId { get; set; }

            public string SetName { get; set; }

            public int Tier { get; set; }

            public int RequiredGearLevel { get; set; }

            public int RequiredBreakLevel { get; set; }

            public string Description { get; set; }
        }
        private sealed class CurrentGearPieceRecord
        {
            public int Id { get; set; }

            public int SuitId { get; set; }

            public int Type { get; set; }

            public int Quality { get; set; }

            public string Name { get; set; }

            public string SetName { get; set; }

            public string SlotName { get; set; }

            public int GearAttack { get; set; }

            public int GearHp { get; set; }

            public int GearAttackPercent { get; set; }

            public int GearHpPercent { get; set; }

            public int SpecialAttributeId { get; set; }

            public int SpecialAttributeValue { get; set; }

            public string SpecialAttributeName { get; set; }

            public int Level40GearAttack { get; set; }

            public int Level40GearHp { get; set; }

            public int Level40GearAttackPercent { get; set; }

            public int Level40GearHpPercent { get; set; }

            public int Level40SpecialAttributeValue { get; set; }

            public int Break25GearAttack { get; set; }

            public int Break25GearHp { get; set; }

            public int Break25GearAttackPercent { get; set; }

            public int Break25GearHpPercent { get; set; }

            public int Break25SpecialAttributeValue { get; set; }

            public int MythicGearAttack { get; set; }

            public int MythicGearHp { get; set; }

            public int MythicGearAttackPercent { get; set; }

            public int MythicGearHpPercent { get; set; }

            public int MythicSpecialAttributeValue { get; set; }

            public string BaseAttributes { get; set; }

            public string BasePower { get; set; }
        }
        private sealed class GearUpgradeTotalRecord
        {
            public string Scope { get; set; }

            public int GearEnhanceRunes { get; set; }

            public int SolarGoldIngots { get; set; }

            public int Rubies { get; set; }

            public int LegendaryPromotionStones
            {
                get;
                set;
            }

            public int MythicPromotionStones
            {
                get;
                set;
            }
        }
        private sealed class GearBreakCostRecord
        {
            public int BreakLevel { get; set; }

            public int SolarGoldIngots { get; set; }

            public int GearEnhanceRunes { get; set; }

            public int Rubies { get; set; }

            public int LegendaryPromotionStones
            {
                get;
                set;
            }

            public int MythicPromotionStones
            {
                get;
                set;
            }
        }
        private sealed class GearLevelCostRecord
        {
            public int Level { get; set; }

            public int GearEnhanceRunes { get; set; }
        }
    }
}









































