﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace SysBot.Pokemon
{
    public class TradeExtensions<T> where T : PKM, new()
    {
        private static readonly object _syncLog = new();
        public static bool CoordinatesSet = false;
        public static ulong CoordinatesOffset = 0;
        public static byte[] XCoords = { 0 };
        public static byte[] YCoords = { 0 };
        public static byte[] ZCoords = { 0 };
        public static readonly string[] Characteristics =
        {
            "Takes plenty of siestas",
            "Likes to thrash about",
            "Capable of taking hits",
            "Alert to sounds",
            "Mischievous",
            "Somewhat vain",
        };

        public static readonly int[] Amped = { 3, 4, 2, 8, 9, 19, 22, 11, 13, 14, 0, 6, 24 };
        public static readonly int[] LowKey = { 1, 5, 7, 10, 12, 15, 16, 17, 18, 20, 21, 23 };
        public static readonly ushort[] ShinyLock = {  (ushort)Species.Victini, (ushort)Species.Keldeo, (ushort)Species.Volcanion, (ushort)Species.Cosmog, (ushort)Species.Cosmoem, (ushort)Species.Magearna, (ushort)Species.Marshadow, (ushort)Species.Eternatus,
                                                    (ushort)Species.Kubfu, (ushort)Species.Urshifu, (ushort)Species.Zarude, (ushort)Species.Glastrier, (ushort)Species.Spectrier, (ushort)Species.Calyrex };

        public static bool ShinyLockCheck(ushort species, string form, string ball = "")
        {
            if (ShinyLock.Contains(species))
                return true;
            else if (form != "" && (species is (int)Species.Zapdos or (int)Species.Moltres or (int)Species.Articuno))
                return true;
            else if (ball.Contains("Beast") && (species is (int)Species.Poipole or (int)Species.Naganadel))
                return true;
            else if (typeof(T) == typeof(PB8) && (species is (int)Species.Manaphy or (int)Species.Mew or (int)Species.Jirachi))
                return true;
            else if (species is (int)Species.Pikachu && form != "" && form != "-Partner")
                return true;
            else if ((species is (ushort)Species.Zacian or (ushort)Species.Zamazenta) && !ball.Contains("Cherish"))
                return true;
            else return false;
        }

        public static Ball[] GetLegalBalls(string showdown)
        {
            var showdownList = showdown.Replace("\r", "").Split('\n').ToList();
            showdownList.RemoveAll(x => x.Contains("Level") || x.Contains("- "));
            showdown = string.Join("\r\n", showdownList);

            var set = new ShowdownSet(showdown);
            var templ = AutoLegalityWrapper.GetTemplate(set);
            var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
            var pk = (T)sav.GetLegal(templ, out string res);
            
            if (res != "Regenerated")
            {
                Base.LogUtil.LogError($"Failed to generate a template for legal Poke Balls: \n{showdown}", "[GetLegalBalls]");
                return new Ball[1];
            }

            var legalBalls = BallApplicator.GetLegalBalls(pk).ToList();
            if (!legalBalls.Contains(Ball.Master))
            {
                showdownList.Insert(1, "Ball: Master");
                set = new ShowdownSet(string.Join("\n", showdownList));
                templ = AutoLegalityWrapper.GetTemplate(set);
                pk = (T)sav.GetLegal(templ, out res);
                if (res == "Regenerated")
                    legalBalls.Add(Ball.Master);
            }
            return legalBalls.ToArray();
        }

        public static A EnumParse<A>(string input) where A : struct, Enum => !Enum.TryParse(input, true, out A result) ? new() : result;

        public static bool HasAdName(T pk, out string ad)
        {
            string pattern = @"(YT$)|(YT\w*$)|(Lab$)|(\.\w*$|\.\w*\/)|(TV$)|(PKHeX)|(FB:)|(AuSLove)|(ShinyMart)|(Blainette)|(\ com)|(\ org)|(\ net)|(2DOS3)|(PPorg)|(Tik\wok$)|(YouTube)|(IG:)|(TTV\ )|(Tools)|(JokersWrath)|(bot$)|(PKMGen)|(TheHighTable)"; bool ot = Regex.IsMatch(pk.OT_Name, pattern, RegexOptions.IgnoreCase);
            bool nick = Regex.IsMatch(pk.Nickname, pattern, RegexOptions.IgnoreCase);
            ad = ot ? pk.OT_Name : nick ? pk.Nickname : "";
            return ot || nick;
        }

        public static void DittoTrade(PKM pkm)
        {
            var dittoStats = new string[] { "atk", "spe", "spa" };
            var nickname = pkm.Nickname.ToLower();
            pkm.StatNature = pkm.Nature;
            pkm.Met_Location = pkm switch
            {
                PB8 => 400,
                PK9 => 28,
                _ => 162, // PK8
            };

            pkm.Met_Level = pkm switch
            {
                PB8 => 29,
                PK9 => 34,
                _ => pkm.Met_Level,
            };

            if (pkm is PK9 pk9)
            {
                pk9.Obedience_Level = (byte)pk9.Met_Level;
                pk9.TeraTypeOriginal = MoveType.Normal;
                pk9.TeraTypeOverride = (MoveType)19;
            }
            pkm.Ball = 21;
            pkm.IVs = new int[] { 31, nickname.Contains(dittoStats[0]) ? 0 : 31, 31, nickname.Contains(dittoStats[1]) ? 0 : 31, nickname.Contains(dittoStats[2]) ? 0 : 31, 31 };
            pkm.ClearHyperTraining();
            TrashBytes(pkm, new LegalityAnalysis(pkm));
        }

        public static void EggTrade(PKM pk, IBattleTemplate template)
        {
            pk.IsNicknamed = true;
            pk.Nickname = pk.Language switch
            {
                1 => "タマゴ",
                3 => "Œuf",
                4 => "Uovo",
                5 => "Ei",
                7 => "Huevo",
                8 => "알",
                9 or 10 => "蛋",
                _ => "Egg",
            };

            pk.IsEgg = true;
            pk.Egg_Location = pk switch
            {
                PB8 => 60010,
                PK9 => 30023,
                _ => 60002, //PK8
            };

            pk.MetDate = DateOnly.FromDateTime(DateTime.Today);
            pk.EggMetDate = pk.MetDate;
            pk.HeldItem = 0;
            pk.CurrentLevel = 1;
            pk.EXP = 0;
            pk.Met_Level = 1;
            pk.Met_Location = pk switch
            {
                PB8 => 65535,
                PK9 => 0,
                _ => 30002, //PK8
            };

            pk.CurrentHandler = 0;
            pk.OT_Friendship = 1;
            pk.HT_Name = "";
            pk.HT_Friendship = 0;
            pk.ClearMemories();
            pk.StatNature = pk.Nature;
            pk.SetEVs(new int[] { 0, 0, 0, 0, 0, 0 });

            pk.SetMarking(0, 0);
            pk.SetMarking(1, 0);
            pk.SetMarking(2, 0);
            pk.SetMarking(3, 0);
            pk.SetMarking(4, 0);
            pk.SetMarking(5, 0);

            pk.ClearRelearnMoves();

            if (pk is PK8 pk8)
            {
                pk8.HT_Language = 0;
                pk8.HT_Gender = 0;
                pk8.HT_Memory = 0;
                pk8.HT_Feeling = 0;
                pk8.HT_Intensity = 0;
                pk8.DynamaxLevel = pk8.GetSuggestedDynamaxLevel(pk8, 0);
            }
            else if (pk is PB8 pb8)
            {
                pb8.HT_Language = 0;
                pb8.HT_Gender = 0;
                pb8.HT_Memory = 0;
                pb8.HT_Feeling = 0;
                pb8.HT_Intensity = 0;
                pb8.DynamaxLevel = pb8.GetSuggestedDynamaxLevel(pb8, 0);
            }
            else if (pk is PK9 pk9)
            {
                pk9.HT_Language = 0;
                pk9.HT_Gender = 0;
                pk9.HT_Memory = 0;
                pk9.HT_Feeling = 0;
                pk9.HT_Intensity = 0;
                pk9.Obedience_Level = 1;
                pk9.Version = 0;
                pk9.BattleVersion = 0;
                pk9.TeraTypeOverride = (MoveType)19;
            }

            pk = TrashBytes(pk);
            var la = new LegalityAnalysis(pk);
            var enc = la.EncounterMatch;
            pk.SetSuggestedRibbons(template, enc, true, HandlerType.Verbose.GetTracebackHandler());
            pk.SetSuggestedMoves();
            la = new LegalityAnalysis(pk);
            enc = la.EncounterMatch;
            pk.CurrentFriendship = enc is IHatchCycle h ? h.EggCycles : pk.PersonalInfo.HatchCycles;

            Span<ushort> relearn = stackalloc ushort[4];
            la.GetSuggestedRelearnMoves(relearn, enc);
            pk.SetRelearnMoves(relearn);

            pk.Move1_PPUps = pk.Move2_PPUps = pk.Move3_PPUps = pk.Move4_PPUps = 0;
            pk.SetMaximumPPCurrent(pk.Moves);
            pk.SetSuggestedHyperTrainingData();
        }

        public static void EncounterLogs(PKM pk, string filepath = "")
        {
            if (filepath == "")
                filepath = "EncounterLogPretty.txt";

            if (!File.Exists(filepath))
            {
                var blank = "Totals: 0 Pokémon, 0 Eggs, 0 ★, 0 ■, 0 🎀\n_________________________________________________\n";
                File.WriteAllText(filepath, blank);
            }

            lock (_syncLog)
            {
                bool mark = false;
                if (pk is PK8)
                    mark = pk is PK8 pk8 && pk8.HasEncounterMark();
                if (pk is PK9)
                    mark = pk is PK9 pk9 && pk9.HasEncounterMark();

                var content = File.ReadAllText(filepath).Split('\n').ToList();
                var splitTotal = content[0].Split(',');
                content.RemoveRange(0, 3);

                int pokeTotal = int.Parse(splitTotal[0].Split(' ')[1]) + 1;
                int eggTotal = int.Parse(splitTotal[1].Split(' ')[1]) + (pk.IsEgg ? 1 : 0);
                int starTotal = int.Parse(splitTotal[2].Split(' ')[1]) + (pk.IsShiny && pk.ShinyXor > 0 ? 1 : 0);
                int squareTotal = int.Parse(splitTotal[3].Split(' ')[1]) + (pk.IsShiny && pk.ShinyXor == 0 ? 1 : 0);
                int markTotal = int.Parse(splitTotal[4].Split(' ')[1]) + (mark ? 1 : 0);

                var form = FormOutput(pk.Species, pk.Form, out _);
                var speciesName = $"{SpeciesName.GetSpeciesNameGeneration(pk.Species, pk.Language, 8)}{form}".Replace(" ", "");
                var index = content.FindIndex(x => x.Split(':')[0].Equals(speciesName));

                if (index == -1)
                    content.Add($"{speciesName}: 1, {(pk.IsShiny && pk.ShinyXor > 0 ? 1 : 0)}★, {(pk.IsShiny && pk.ShinyXor == 0 ? 1 : 0)}■, {(mark ? 1 : 0)}🎀, {GetPercent(pokeTotal, 1)}%");

                var length = index == -1 ? 1 : 0;
                for (int i = 0; i < content.Count - length; i++)
                {
                    var sanitized = GetSanitizedEncounterLineArray(content[i]);
                    if (i == index)
                    {
                        int speciesTotal = int.Parse(sanitized[1]) + 1;
                        int stTotal = int.Parse(sanitized[2]) + (pk.IsShiny && pk.ShinyXor > 0 ? 1 : 0);
                        int sqTotal = int.Parse(sanitized[3]) + (pk.IsShiny && pk.ShinyXor == 0 ? 1 : 0);
                        int mTotal = int.Parse(sanitized[4]) + (mark ? 1 : 0);
                        content[i] = $"{speciesName}: {speciesTotal}, {stTotal}★, {sqTotal}■, {mTotal}🎀, {GetPercent(pokeTotal, speciesTotal)}%";
                    }
                    else content[i] = $"{sanitized[0]} {sanitized[1]}, {sanitized[2]}★, {sanitized[3]}■, {sanitized[4]}🎀, {GetPercent(pokeTotal, int.Parse(sanitized[1]))}%";
                }

                content.Sort();
                string totalsString =
                    $"Totals: {pokeTotal} Pokémon, " +
                    $"{eggTotal} Eggs ({GetPercent(pokeTotal, eggTotal)}%), " +
                    $"{starTotal} ★ ({GetPercent(pokeTotal, starTotal)}%), " +
                    $"{squareTotal} ■ ({GetPercent(pokeTotal, squareTotal)}%), " +
                    $"{markTotal} 🎀 ({GetPercent(pokeTotal, markTotal)}%)" +
                    "\n_________________________________________________\n";
                content.Insert(0, totalsString);
                File.WriteAllText(filepath, string.Join("\n", content));
            }
        }

        public static void EncounterScaleLogs(PK9 pk, string filepath = "")
        {
            if (filepath == "")
                filepath = "EncounterScaleLogPretty.txt";

            if (!File.Exists(filepath))
            {
                var blank = "Totals: 0 Pokémon, 0 Mini, 0 Jumbo, 0 Miscellaneous\n_________________________________________________\n";
                File.WriteAllText(filepath, blank);
            }

            lock (_syncLog)
            {
                var content = File.ReadAllText(filepath).Split('\n').ToList();
                var splitTotal = content[0].Split(',');
                content.RemoveRange(0, 3);

                bool isMini = pk.Scale == 0;
                bool isJumbo = pk.Scale == 255;
                bool isMisc = pk.Scale > 0 && pk.Scale < 255;
                int pokeTotal = int.Parse(splitTotal[0].Split(' ')[1]) + 1;
                int miniTotal = int.Parse(splitTotal[1].Split(' ')[1]) + (isMini ? 1 : 0);
                int jumboTotal = int.Parse(splitTotal[2].Split(' ')[1]) + (isJumbo ? 1 : 0);
                int otherTotal = int.Parse(splitTotal[3].Split(' ')[1]) + (isMisc ? 1 : 0);

                var form = FormOutput(pk.Species, pk.Form, out _);
                var speciesName = $"{SpeciesName.GetSpeciesNameGeneration(pk.Species, pk.Language, 9)}{form}".Replace(" ", "");
                var index = content.FindIndex(x => x.Split(':')[0].Equals(speciesName));

                if (index == -1)
                    content.Add($"{speciesName}: 1, {(isMini ? 1 : 0)} Mini, {(isJumbo ? 1 : 0)} Jumbo, {(isMisc ? 1 : 0)} Miscellaneous");

                var length = index == -1 ? 1 : 0;
                for (int i = 0; i < content.Count - length; i++)
                {
                    var sanitized = GetSanitizedEncounterScaleArray(content[i]);
                    if (i == index)
                    {
                        int speciesTotal = int.Parse(sanitized[1]) + 1;
                        int miTotal = int.Parse(sanitized[2]) + (isMini ? 1 : 0);
                        int juTotal = int.Parse(sanitized[3]) + (isJumbo ? 1 : 0);
                        int otTotal = int.Parse(sanitized[4]) + (isMisc ? 1 : 0);
                        content[i] = $"{speciesName}: {speciesTotal}, {miTotal} Mini, {juTotal} Jumbo, {otTotal} Miscellaneous";
                    }
                    else content[i] = $"{sanitized[0]} {sanitized[1]}, {sanitized[2]} Mini, {sanitized[3]} Jumbo, {sanitized[4]} Miscellaneous";
                }

                content.Sort();
                string totalsString =
                    $"Totals: {pokeTotal} Pokémon, " +
                    $"{miniTotal} Mini ({GetPercent(pokeTotal, miniTotal)}%), " +
                    $"{jumboTotal} Jumbo ({GetPercent(pokeTotal, jumboTotal)}%), " +
                    $"{otherTotal} Miscellaneous ({GetPercent(pokeTotal, otherTotal)}%)" +
                    "\n_________________________________________________\n";
                content.Insert(0, totalsString);
                File.WriteAllText(filepath, string.Join("\n", content));
            }
        }

        private static string GetPercent(int total, int subtotal) => (100.0 * ((double)subtotal / total)).ToString("N2", NumberFormatInfo.InvariantInfo);

        private static string[] GetSanitizedEncounterScaleArray(string content)
        {
            var replace = new Dictionary<string, string> { { ",", "" }, { " Mini", "" }, { " Jumbo", "" }, { " Miscellaneous", "" }, { "%", "" } };
            return replace.Aggregate(content, (old, cleaned) => old.Replace(cleaned.Key, cleaned.Value)).Split(' ');
        }

        private static string[] GetSanitizedEncounterLineArray(string content)
        {
            var replace = new Dictionary<string, string> { { ",", "" }, { "★", "" }, { "■", "" }, { "🎀", "" }, { "%", "" } };
            return replace.Aggregate(content, (old, cleaned) => old.Replace(cleaned.Key, cleaned.Value)).Split(' ');
        }

        public static PKM TrashBytes(PKM pkm, LegalityAnalysis? la = null)
        {
            var pkMet = (T)pkm.Clone();
            if (pkMet.Version is not (int)GameVersion.GO)
                pkMet.MetDate = DateOnly.FromDateTime(DateTime.Now);

            var analysis = new LegalityAnalysis(pkMet);
            var pkTrash = (T)pkMet.Clone();
            if (analysis.Valid)
            {
                pkTrash.IsNicknamed = true;
                pkTrash.Nickname = "MANUMANUMANU";
                pkTrash.SetDefaultNickname(la ?? new LegalityAnalysis(pkTrash));
            }

            if (new LegalityAnalysis(pkTrash).Valid)
                pkm = pkTrash;
            else if (analysis.Valid)
                pkm = pkMet;
            return pkm;
        }

        public static T CherishHandler(MysteryGift mg, ITrainerInfo info)
        {
            var result = EntityConverterResult.None;
            var mgPkm = mg.ConvertToPKM(info);
            bool canConvert = EntityConverter.IsConvertibleToFormat(mgPkm, info.Generation);
            mgPkm = canConvert ? EntityConverter.ConvertToType(mgPkm, typeof(T), out result) : mgPkm;

            if (mgPkm is not null && result is EntityConverterResult.Success)
            {
                var enc = new LegalityAnalysis(mgPkm).EncounterMatch;
                mgPkm.SetHandlerandMemory(info, enc, HandlerType.Verbose.GetTracebackHandler());

                if (mgPkm.TID16 is 0 && mgPkm.SID16 is 0)
                {
                    mgPkm.TID16 = info.TID16;
                    mgPkm.SID16 = info.SID16;
                }

                mgPkm.CurrentLevel = mg.LevelMin;
                if (mgPkm.Species is (ushort)Species.Giratina && mgPkm.Form > 0)
                    mgPkm.HeldItem = 112;
                else if (mgPkm.Species is (ushort)Species.Silvally && mgPkm.Form > 0)
                    mgPkm.HeldItem = mgPkm.Form + 903;
                else mgPkm.HeldItem = 0;
            }
            else return new();

            mgPkm = TrashBytes((T)mgPkm);
            var la = new LegalityAnalysis(mgPkm);
            if (!la.Valid)
            {
                mgPkm.SetRandomIVs(6);
                var text = ShowdownParsing.GetShowdownText(mgPkm);
                var set = new ShowdownSet(text);
                var template = AutoLegalityWrapper.GetTemplate(set);
                var pk = AutoLegalityWrapper.GetLegal(info, template, out _);
                pk.SetAllTrainerData(info);
                return (T)pk;
            }
            else return (T)mgPkm;
        }

        public static string PokeImg(PKM pkm, bool canGmax, bool fullSize)
        {
            bool md = false;
            bool fd = false;
            string[] baseLink;
            if (fullSize)
                baseLink = "https://raw.githubusercontent.com/zyro670/HomeImages/master/512x512/poke_capture_0001_000_mf_n_00000000_f_n.png".Split('_');
            else baseLink = "https://raw.githubusercontent.com/zyro670/HomeImages/master/128x128/poke_capture_0001_000_mf_n_00000000_f_n.png".Split('_');

            if (Enum.IsDefined(typeof(GenderDependent), pkm.Species) && !canGmax && pkm.Form is 0)
            {
                if (pkm.Gender == 0 && pkm.Species != (ushort)Species.Torchic)
                    md = true;
                else fd = true;
            }

            byte form = pkm.Species switch
            {
                (ushort)Species.Sinistea or (ushort)Species.Polteageist or (ushort)Species.Rockruff or (ushort)Species.Mothim => 0,
                (ushort)Species.Alcremie when pkm.IsShiny || canGmax => 0,
                _ => pkm.Form,

            };

            if (pkm.Species is (ushort)Species.Sneasel)
            {
                if (pkm.Gender is 0)
                    md = true;
                else fd = true;
            }

            if (pkm.Species is (ushort)Species.Basculegion)
            {
                if (pkm.Gender is 0)
                {
                    md = true;
                    pkm.Form = 0;
                }
                else
                    pkm.Form = 1;

                string s = pkm.IsShiny ? "r" : "n";
                string g = md && pkm.Gender is not 1 ? "md" : "fd";
                return $"https://raw.githubusercontent.com/zyro670/HomeImages/master/128x128/poke_capture_0" + $"{pkm.Species}" + "_00" + $"{pkm.Form}" + "_" + $"{g}" + "_n_00000000_f_" + $"{s}" + ".png";
            }

            baseLink[2] = pkm.Species < 10 ? $"000{pkm.Species}" : pkm.Species < 100 && pkm.Species > 9 ? $"00{pkm.Species}" : pkm.Species > 999 ? $"{pkm.Species}" : $"0{pkm.Species}";
            baseLink[3] = pkm.Form < 10 ? $"00{form}" : $"0{form}";
            baseLink[4] = pkm.PersonalInfo.OnlyFemale ? "fo" : pkm.PersonalInfo.OnlyMale ? "mo" : pkm.PersonalInfo.Genderless ? "uk" : fd ? "fd" : md ? "md" : "mf";
            baseLink[5] = canGmax ? "g" : "n";
            baseLink[6] = "0000000" + (pkm.Species == (ushort)Species.Alcremie && !canGmax ? pkm.Data[0xE4] : 0);
            baseLink[8] = pkm.IsShiny ? "r.png" : "n.png";
            return string.Join("_", baseLink);
        }

        public static string FormOutput(ushort species, byte form, out string[] formString)
        {
            var strings = GameInfo.GetStrings("en");
            formString = FormConverter.GetFormList(species, strings.Types, strings.forms, GameInfo.GenderSymbolASCII, typeof(T) == typeof(PK9) ? EntityContext.Gen9 : EntityContext.Gen4);
            if (formString.Length is 0)
                return string.Empty;

            formString[0] = "";
            if (form >= formString.Length)
                form = (byte)(formString.Length - 1);
            return formString[form].Contains('-') ? formString[form] : formString[form] == "" ? "" : $"-{formString[form]}";
        }

        public static PK8 SWSHTrade = new();
        public static PB8 BDSPTrade = new();
        public static PA8 LATrade = new();
        public static PK9 SVTrade = new();
    }
}