// Standalone tests use the real workbook code, enum and EPPlus, without loading Unity.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameFramework.Localization;
using OfficeOpenXml;
using UGF.EditorTools;
namespace UGF.EditorTools { public class LocalizationText { public string Key; public string Value; public bool Locked; } }
internal static class TranslationWorkbookTests
{
    static int count;
    static void Check(bool pass, string name) { if (!pass) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); count++; }
    static void Reject(Action action, string name) { try { action(); } catch (InvalidDataException) { Check(true, name); return; } throw new Exception("Expected rejection: " + name); }
    static Dictionary<Language, List<LocalizationText>> Empty() => new Dictionary<Language, List<LocalizationText>>();
    static TranslationTable Sample()
    {
        var t = new TranslationTable { SourceLanguage = Language.English };
        t.Languages.AddRange(new[] { Language.English, Language.Japanese, Language.PortugueseBrazil, Language.PortuguesePortugal });
        foreach (var pair in new[] { new[] { "ui.start", "Let's go" }, new[] { "ui.level", "<b>Level {0}</b>\n" } })
        {
            var r = new TranslationRow { Key = pair[0], Source = pair[1], OriginalSource = pair[1], Locked = true };
            r.Values[Language.Japanese] = pair[0] == "ui.start" ? "さあ、行こう" : "<b>レベル {0}</b>\n";
            r.Values[Language.PortugueseBrazil] = pair[0] == "ui.start" ? "Vamos!" : "<b>Nível {0}</b>\n";
            r.Values[Language.PortuguesePortugal] = pair[0] == "ui.start" ? "Vamos lá!" : "<b>Nível {0}</b>\n";
            t.Rows.Add(r);
        }
        return t;
    }
    static byte[] Edit(byte[] input, Action<ExcelPackage> edit)
    { using (var stream = new MemoryStream(input)) using (var excel = new ExcelPackage(stream)) { edit(excel); return excel.GetAsByteArray(); } }
    public static int Main()
    {
        try
        {
            string temp = Path.GetFullPath("Temp/LocalizationVerification/Fixtures"); Directory.CreateDirectory(temp);
            byte[] bytes = TranslationWorkbook.Write(Sample()); var t = TranslationWorkbook.Read(bytes);
            Check(t.Rows[1].Values[Language.Japanese] == "<b>レベル {0}</b>\n" && t.Rows[0].Locked, "Unicode, multiline, locked round trip");
            Check(t.Languages.Count == 4 && t.Rows[0].Values[Language.PortugueseBrazil] != t.Rows[0].Values[Language.PortuguesePortugal], "Portuguese regions distinct");
            using (var s = new MemoryStream(bytes)) using (var e = new ExcelPackage(s)) Check(e.Workbook.Worksheets["Translations"].Column(1).Hidden, "Key hidden");
            var reordered = TranslationWorkbook.Read(Edit(bytes, e => {
                var s = e.Workbook.Worksheets["Translations"];
                for (int r = 1; r <= 3; r++) { var v = s.Cells[r, 3].Value; s.Cells[r, 3].Value = s.Cells[r, 5].Value; s.Cells[r, 5].Value = v; }
                for (int c = 1; c <= s.Dimension.End.Column; c++) { var v = s.Cells[2, c].Value; s.Cells[2, c].Value = s.Cells[3, c].Value; s.Cells[3, c].Value = v; }
            }));
            Check(reordered.Rows[1].Key == "ui.start" && reordered.Rows[1].Values[Language.Japanese] == "さあ、行こう", "row and column order independent");
            Reject(() => TranslationWorkbook.Read(Edit(bytes, e => e.Workbook.Worksheets["Translations"].Cells[1, 3].Value = "Portuguese")), "unknown header rejected");
            Reject(() => TranslationWorkbook.Read(Edit(bytes, e => e.Workbook.Worksheets["Translations"].Cells[1, 3].Value = "English")), "duplicate header rejected");
            Reject(() => TranslationWorkbook.Read(Edit(bytes, e => e.Workbook.Worksheets["Translations"].Cells[3, 1].Value = "ui.start")), "duplicate Key rejected");
            Reject(() => TranslationWorkbook.Read(Edit(bytes, e => e.Workbook.Worksheets["Translations"].Cells[2, 1].Value = "changed")), "modified Key rejected");
            Reject(() => TranslationWorkbook.Read(Edit(bytes, e => e.Workbook.Worksheets["Translations"].DeleteRow(3))), "deleted row rejected");
            Reject(() => TranslationWorkbook.Read(Edit(bytes, e => e.Workbook.Worksheets["Translations"].Cells[2, 3].Formula = "1+1")), "formula rejected");
            Reject(() => TranslationWorkbook.Read(Edit(bytes, e => e.Workbook.Worksheets["Translations"].DeleteColumn(3))), "deleted language column rejected");
            var validated = TranslationWorkbook.Validate(t, Empty()); Check(validated[Language.Japanese].Count == 2, "complete table validates");
            t.Rows[0].Values[Language.Japanese] = "";
            Check(TranslationWorkbook.Validate(t, validated)[Language.Japanese][0].Value == "さあ、行こう", "blank preserves old translation");
            Reject(() => TranslationWorkbook.Validate(t, Empty()), "missing translation blocks output");
            t = Sample(); t.Rows[0].Source = "Go now";
            Reject(() => TranslationWorkbook.Validate(t, Empty()), "source change requires review");
            t.Rows[0].Status = TranslationWorkbook.Reviewed;
            Check(TranslationWorkbook.Validate(t, Empty())[Language.English][0].Value == "Go now", "review accepted");
            var prior = TranslationWorkbook.Validate(t, Empty()); t.Rows[0].Source = "Go again";
            Reject(() => TranslationWorkbook.Validate(t, prior), "second source change cannot reuse old review");
            Check(TranslationWorkbook.ValidateText("{0} {1}", "{1} {0}") == null, "placeholder reorder valid");
            Check(TranslationWorkbook.ValidateText("Level {0}", "レベル") != null, "missing placeholder detected");
            Check(TranslationWorkbook.ValidateText("<b><i>A</i></b>", "<b><i>B</b></i>") != null, "crossed tags detected");
            Check(TranslationWorkbook.ValidateText("A\nB", "AB") != null, "line break checked");
            Check(TranslationWorkbook.ValidateText("A ", "B") != null, "whitespace checked");
            var old = Sample(); old.Rows[1].Locked = false; string rev = old.Revision;
            var merged = TranslationWorkbook.Merge(new List<LocalizationText> { new LocalizationText { Key="ui.start", Value="Go now", Locked=true }, new LocalizationText { Key="ui.new", Value="New" } }, Language.English, new[] { Language.French }, old, Empty(), new HashSet<string>{ "ui.start", "ui.new" });
            Check(merged.Rows[0].Values[Language.Japanese] == "さあ、行こう" && merged.Rows[0].Status == TranslationWorkbook.Review, "merge keeps translations and flags changed source");
            Check(merged.Rows.Count == 2 && merged.Rows.All(r => r.Key != "ui.level"), "unscanned unlocked rows removed");
            Check(merged.Languages.Contains(Language.French) && merged.Languages.Contains(Language.Japanese), "language added without deleting old columns");
            Check(merged.Revision != rev, "new export revision");
            var orphanLegacy = new Dictionary<Language, List<LocalizationText>> { [Language.Japanese] = new List<LocalizationText> { new LocalizationText { Key = "legacy.only", Value = "保留", Locked = true } } };
            var migration = TranslationWorkbook.Merge(new List<LocalizationText> { new LocalizationText { Key = "ui.start", Value = "Start" } }, Language.English, new[] { Language.Japanese }, null, orphanLegacy, new HashSet<string> { "ui.start" });
            Check(migration.Rows.Any(r => r.Key == "legacy.only" && r.Status == TranslationWorkbook.Retired && r.Values[Language.Japanese] == "保留"), "target-only legacy rows retained during migration");
            foreach (string status in new[] { TranslationWorkbook.Normal, TranslationWorkbook.Review, TranslationWorkbook.Reviewed, TranslationWorkbook.Retired })
            {
                var cleanup = Sample();
                cleanup.Rows[1].Locked = false;
                cleanup.Rows[1].Status = status;
                cleanup = TranslationWorkbook.Merge(new List<LocalizationText>(), Language.English, new[] { Language.Japanese }, cleanup, Empty(), new HashSet<string>());
                Check(cleanup.Rows.Count == 1 && cleanup.Rows[0].Key == "ui.start" && cleanup.Rows[0].Values[Language.Japanese] == "さあ、行こう", "empty scan removes unlocked " + status + " and preserves locked translation");
                Check(TranslationWorkbook.Read(TranslationWorkbook.Write(cleanup)).Rows.Count == 1, "cleaned workbook round trip " + status);
            }
            var unscanned = Sample(); unscanned.Rows[1].Locked = false;
            Check(TranslationWorkbook.Merge(new List<LocalizationText>(), Language.English, new[] { Language.Japanese }, unscanned, Empty(), null).Rows.Count == 2, "no scan preserves unlocked rows");
            var unlocked = TranslationWorkbook.Merge(new List<LocalizationText> { new LocalizationText { Key = "ui.start", Value = "Let's go", Locked = false } }, Language.English, new[] { Language.Japanese }, Sample(), Empty(), new HashSet<string>());
            Check(unlocked.Rows.Count == 1 && unlocked.Rows[0].Key == "ui.level", "current unlock takes effect before cleanup");
            var emptyTable = Sample(); emptyTable.Rows.ForEach(r => r.Locked = false);
            emptyTable = TranslationWorkbook.Merge(new List<LocalizationText>(), Language.English, new[] { Language.Japanese }, emptyTable, Empty(), new HashSet<string>());
            Check(TranslationWorkbook.Read(TranslationWorkbook.Write(emptyTable)).Rows.Count == 0, "all rows can be cleaned and exported");
            var repeated = TranslationWorkbook.Merge(new List<LocalizationText>(), Language.English, new[] { Language.Japanese }, emptyTable, orphanLegacy, null);
            Check(repeated.Rows.Count == 0, "legacy rows cannot return after cleanup");
            string legacy = Path.Combine(temp, "Japanese.xlsx"); File.WriteAllBytes(legacy, TranslationWorkbook.WriteLegacy(validated[Language.Japanese]));
            var loaded = TranslationWorkbook.ReadLegacy(legacy);
            Check(loaded.Count == 2 && loaded[0].Locked && loaded[0].Value == "さあ、行こう", "legacy Excel remains compatible");
            string a = Path.Combine(temp, "first.txt"), b = Path.Combine(temp, "second.txt"); File.WriteAllText(a, "original"); File.WriteAllText(b, "locked");
            using (new FileStream(b, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                bool blocked = false; try { TranslationWorkbook.Commit(new Dictionary<string, byte[]> { [a]=new byte[]{1}, [b]=new byte[]{2} }); } catch(IOException) { blocked=true; }
                Check(blocked && File.ReadAllText(a) == "original", "locked output leaves originals intact");
            }
            TranslationWorkbook.Commit(new Dictionary<string, byte[]> { [a]=new byte[]{1}, [b]=new byte[]{2} });
            Check(File.ReadAllBytes(a)[0] == 1 && File.ReadAllBytes(b)[0] == 2, "multi-file commit");
            var projectLanguages = Directory.GetFiles("AAAGameData/Languages", "*.xlsx")
                .Where(p => Enum.TryParse(Path.GetFileNameWithoutExtension(p), out Language language) && language != Language.Unspecified)
                .ToDictionary(p => (Language)Enum.Parse(typeof(Language), Path.GetFileNameWithoutExtension(p)), TranslationWorkbook.ReadLegacy);
            var projectTable = TranslationWorkbook.Merge(projectLanguages[Language.English], Language.English,
                projectLanguages.Keys, null, projectLanguages, null);
            byte[] projectBytes = TranslationWorkbook.Write(projectTable);
            File.WriteAllBytes(Path.Combine(temp, "ProjectLocalization.xlsx"), projectBytes);
            var projectRoundTrip = TranslationWorkbook.Read(projectBytes);
            Check(projectRoundTrip.Rows.Count == projectLanguages.Values.SelectMany(rows => rows.Select(r => r.Key)).Distinct().Count() &&
                projectRoundTrip.Languages.Count == projectLanguages.Count &&
                projectRoundTrip.Rows.All(row => projectLanguages.All(pair => pair.Key == Language.English ?
                    row.Source == (pair.Value.Find(r => r.Key == row.Key)?.Value ?? "") :
                    row.Values[pair.Key] == (pair.Value.Find(r => r.Key == row.Key)?.Value ?? ""))), "real project workbook round trip preserves all translations");
            Console.WriteLine("PROJECT: " + projectRoundTrip.Rows.Count + " rows, " + projectRoundTrip.Languages.Count + " languages");
            Console.WriteLine("ALL " + count + " TESTS PASSED"); return 0;
        }
        catch(Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}