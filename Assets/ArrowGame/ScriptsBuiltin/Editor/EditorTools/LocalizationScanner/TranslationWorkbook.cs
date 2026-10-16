using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GameFramework.Localization;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Controls;
using OfficeOpenXml.Style;

namespace UGF.EditorTools
{
    // Pure workbook operations: no AssetDatabase calls, safe to exercise outside the Editor.
    internal sealed class TranslationRow
    {
        public string Key;
        public string Source = "";
        public string OriginalSource = "";
        public string Note = "";
        public string Status = TranslationWorkbook.Normal;
        public bool Locked;
        public int ExcelRow;
        public readonly Dictionary<Language, string> Values = new Dictionary<Language, string>();
    }

    internal sealed class TranslationTable
    {
        public Language SourceLanguage;
        public string Revision = Guid.NewGuid().ToString("N");
        public readonly List<Language> Languages = new List<Language>();
        public readonly List<TranslationRow> Rows = new List<TranslationRow>();
    }

    internal static class TranslationWorkbook
    {
        internal const string Normal = "正常";
        internal const string Review = "待复核";
        internal const string Reviewed = "已复核";
        internal const string Retired = "待清理";
        private static readonly string[] ExtraHeaders = { "说明", "状态", "锁定", "_Source" };

        public static byte[] ReadBytes(string path)
        {
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var memory = new MemoryStream())
            {
                file.CopyTo(memory);
                return memory.ToArray();
            }
        }

        public static TranslationTable Read(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            using (var excel = new ExcelPackage(stream))
            {
                var sheet = excel.Workbook.Worksheets["Translations"];
                var meta = excel.Workbook.Worksheets["_Metadata"];
                if (sheet == null || sheet.Dimension == null || meta == null || meta.Cells[1, 2].Text != "1")
                    throw new InvalidDataException("不是本工具导出的翻译总表（缺少 Translations / _Metadata 或版本不支持）。");
                var table = new TranslationTable { SourceLanguage = ParseLanguage(meta.Cells[2, 2].Text) };
                table.Revision = meta.Cells[3, 2].Text;
                if (string.IsNullOrEmpty(table.Revision)) throw new InvalidDataException("总表缺少导出版本标识，请重新导出。");
                var headers = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int col = 1; col <= sheet.Dimension.End.Column; col++)
                {
                    string name = sheet.Cells[1, col].Text;
                    if (string.IsNullOrEmpty(name))
                    {
                        if (Enumerable.Range(2, Math.Max(0, sheet.Dimension.End.Row - 1)).Any(r => sheet.Cells[r, col].Value != null))
                            throw new InvalidDataException($"第 {col} 列有内容但缺少表头。");
                        continue;
                    }
                    if (headers.ContainsKey(name)) throw new InvalidDataException($"重复表头：{name}");
                    headers.Add(name, col);
                    if (name == "Key" || ExtraHeaders.Contains(name)) continue;
                    table.Languages.Add(ParseLanguage(name));
                }
                foreach (string required in ExtraHeaders.Concat(new[] { "Key", table.SourceLanguage.ToString() }))
                    if (!headers.ContainsKey(required)) throw new InvalidDataException($"缺少列：{required}，请使用工具导出的完整模板。");
                var expectedLanguages = new HashSet<string>(meta.Cells[2, 3].Text.Split(','), StringComparer.Ordinal);
                if (!expectedLanguages.SetEquals(table.Languages.Select(l => l.ToString())))
                    throw new InvalidDataException("语言列被添加或删除，请在工具中选择语言并重新导出，勿直接修改模板结构。");
                table.Languages.Remove(table.SourceLanguage);
                table.Languages.Insert(0, table.SourceLanguage);
                var keys = new HashSet<string>(StringComparer.Ordinal);
                for (int rowIndex = 2; rowIndex <= sheet.Dimension.End.Row; rowIndex++)
                {
                    if (headers.Values.All(c => sheet.Cells[rowIndex, c].Value == null && string.IsNullOrEmpty(sheet.Cells[rowIndex, c].Formula))) continue;
                    foreach (var col in headers.Values)
                        if (!string.IsNullOrEmpty(sheet.Cells[rowIndex, col].Formula))
                            throw new InvalidDataException($"第 {rowIndex} 行第 {col} 列含公式，请填入纯文本。");
                    string key = sheet.Cells[rowIndex, headers["Key"]].Text;
                    if (string.IsNullOrWhiteSpace(key) || !keys.Add(key)) throw new InvalidDataException($"第 {rowIndex} 行 Key 为空或重复：{key}");
                    var row = new TranslationRow
                    {
                        Key = key,
                        Source = sheet.Cells[rowIndex, headers[table.SourceLanguage.ToString()]].Text,
                        OriginalSource = sheet.Cells[rowIndex, headers["_Source"]].Text,
                        Note = sheet.Cells[rowIndex, headers["说明"]].Text,
                        Status = sheet.Cells[rowIndex, headers["状态"]].Text,
                        Locked = sheet.Cells[rowIndex, headers["锁定"]].Text == "1",
                        ExcelRow = rowIndex
                    };
                    if (!new[] { Normal, Review, Reviewed, Retired }.Contains(row.Status))
                        throw new InvalidDataException($"第 {rowIndex} 行状态无效，请使用：正常、待复核、已复核、待清理。");
                    foreach (var language in table.Languages)
                        row.Values[language] = sheet.Cells[rowIndex, headers[language.ToString()]].Text;
                    table.Rows.Add(row);
                }
                var expectedKeys = new HashSet<string>(StringComparer.Ordinal);
                for (int row = 4; row <= (meta.Dimension?.End.Row ?? 3); row++)
                    if (!string.IsNullOrEmpty(meta.Cells[row, 1].Text)) expectedKeys.Add(meta.Cells[row, 1].Text);
                if (!keys.SetEquals(expectedKeys)) throw new InvalidDataException("总表 Key 被修改、添加或删除；请通过工具重新扫描、导出，勿修改隐藏的 Key 列。");
                if (table.Rows.Count == 0) throw new InvalidDataException("总表没有任何文本。");
                return table;
            }
        }

        private static Language ParseLanguage(string value)
        {
            if (!Enum.TryParse(value, out Language language) || language == Language.Unspecified ||
                !Enum.IsDefined(typeof(Language), language) || language.ToString() != value)
                throw new InvalidDataException($"未知语言表头：{value}。请保留完整枚举名（如 PortugueseBrazil / PortuguesePortugal）。");
            return language;
        }

        public static byte[] Write(TranslationTable table)
        {
            using (var excel = new ExcelPackage())
            {
                var sheet = excel.Workbook.Worksheets.Add("Translations");
                var languages = new[] { table.SourceLanguage }.Concat(table.Languages.Where(l => l != table.SourceLanguage)).Distinct().ToArray();
                var headers = new[] { "Key" }.Concat(languages.Select(l => l.ToString())).Concat(ExtraHeaders).ToArray();
                for (int col = 0; col < headers.Length; col++) sheet.Cells[1, col + 1].Value = headers[col];
                int notesCol = languages.Length + 2;
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    var row = table.Rows[i];
                    int r = i + 2;
                    sheet.Cells[r, 1].Value = row.Key;
                    for (int c = 0; c < languages.Length; c++)
                        sheet.Cells[r, c + 2].Value = languages[c] == table.SourceLanguage ? row.Source :
                            row.Values.TryGetValue(languages[c], out string value) ? value : "";
                    sheet.Cells[r, notesCol].Value = row.Note;
                    sheet.Cells[r, notesCol + 1].Value = row.Status;
                    sheet.Cells[r, notesCol + 2].Value = row.Locked ? "1" : "0";
                    sheet.Cells[r, notesCol + 3].Value = row.OriginalSource;
                    if (row.Status == Review || string.IsNullOrWhiteSpace(row.Source))
                    {
                        sheet.Cells[r, 2, r, headers.Length - 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        sheet.Cells[r, 2, r, headers.Length - 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                    }
                }
                sheet.Cells.Style.Numberformat.Format = "@";
                sheet.Cells.Style.WrapText = true;
                sheet.Cells.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                sheet.Row(1).Style.Font.Bold = true;
                for (int col = 1; col <= headers.Length; col++) sheet.Column(col).Width = 28;
                sheet.Column(1).Hidden = true;
                sheet.Column(headers.Length).Hidden = true;
                sheet.Column(notesCol + 1).Width = 12;
                sheet.Column(notesCol + 2).Width = 8;
                sheet.View.FreezePanes(2, 3);
                sheet.Cells[1, 1, Math.Max(2, table.Rows.Count + 1), headers.Length - 1].AutoFilter = true;
                var statusValidation = sheet.DataValidations.AddListValidation(sheet.Cells[2, notesCol + 1, Math.Max(2, table.Rows.Count + 1), notesCol + 1].Address);
                foreach (string status in new[] { Normal, Review, Reviewed, Retired }) statusValidation.Formula.Values.Add(status);
                statusValidation.ShowErrorMessage = true;
                statusValidation.Error = "请选择正常、待复核、已复核或待清理。";
                var meta = excel.Workbook.Worksheets.Add("_Metadata");
                meta.Cells[1, 1].Value = "Version";
                meta.Cells[1, 2].Value = "1";
                meta.Cells[2, 1].Value = "SourceLanguage";
                meta.Cells[2, 2].Value = table.SourceLanguage.ToString();
                meta.Cells[2, 3].Value = string.Join(",", languages.Select(l => l.ToString()));
                meta.Cells[3, 1].Value = "Keys (do not edit)";
                meta.Cells[3, 2].Value = table.Revision;
                for (int i = 0; i < table.Rows.Count; i++) meta.Cells[i + 4, 1].Value = table.Rows[i].Key;
                meta.Hidden = eWorkSheetHidden.VeryHidden;
                var help = excel.Workbook.Worksheets.Add("填写说明");
                string[] helpLines = {
                    "请在 Translations 工作表填写译文；可以整行排序，不要单独排序某一列。",
                    "不要修改隐藏的 Key、_Source、_Metadata；不要删除行或修改语言表头。",
                    "原文变更后会要求复核译文；确认该行所有语言后，将状态改为已复核。",
                    "空译文保留已有译文；没有旧译文时会阻止生成，不会自动填充英文。",
                    "待清理行仍保留已有内容，不会因一次扫描遗漏而丢失译文。锁定填 1 可保留手动条目。",
                    "占位符（如 {0}）、富文本标签、换行及前后空格必须正确保留；不要填公式。",
                    "保存项目中 AAAGameData/Translations/Localization.xlsx 后，Unity 打开且自动生成启用时会处理。",
                    "也可以在工具中点击导入并生成，选择翻译后的文件。单语言表为生成结果，请统一维护本总表。"
                };
                for (int i = 0; i < helpLines.Length; i++) help.Cells[i + 1, 1].Value = helpLines[i];
                help.Column(1).Width = 110;
                help.Cells.Style.WrapText = true;
                return excel.GetAsByteArray();
            }
        }

        public static List<LocalizationText> ReadLegacy(string path)
        {
            var rows = new List<LocalizationText>();
            if (!File.Exists(path)) return rows;
            using (var stream = new MemoryStream(ReadBytes(path)))
            using (var excel = new ExcelPackage(stream))
            {
                var sheet = excel.Workbook.Worksheets.FirstOrDefault();
                if (sheet?.Dimension == null) return rows;
                var keys = new HashSet<string>(StringComparer.Ordinal);
                for (int r = sheet.Dimension.Start.Row; r <= sheet.Dimension.End.Row; r++)
                {
                    string key = sheet.Cells[r, 2].Text;
                    if (string.IsNullOrWhiteSpace(key)) continue;
                    if (!keys.Add(key)) throw new InvalidDataException($"{Path.GetFileName(path)} 第 {r} 行 Key 重复：{key}");
                    var checkbox = sheet.Drawings[sheet.Cells[r, 1].Address] as ExcelControlCheckBox;
                    rows.Add(new LocalizationText { Key = key, Value = sheet.Cells[r, 3].Text, Locked = checkbox?.Checked == eCheckState.Checked });
                }
            }
            return rows;
        }

        public static TranslationTable Merge(List<LocalizationText> sources, Language sourceLanguage,
            IEnumerable<Language> languages, TranslationTable previous,
            Dictionary<Language, List<LocalizationText>> legacy, HashSet<string> scannedKeys)
        {
            if (previous != null && previous.SourceLanguage != sourceLanguage)
                throw new InvalidDataException("已有总表的主语言不可直接更换，请先备份并移走旧总表，再按新主语言导出。");
            var table = previous ?? new TranslationTable { SourceLanguage = sourceLanguage };
            // Retain deselected language columns: removing a UI option must not delete human translations.
            foreach (var language in new[] { sourceLanguage }.Concat(languages).Distinct())
                if (!table.Languages.Contains(language)) table.Languages.Add(language);
            table.Revision = Guid.NewGuid().ToString("N");
            foreach (var row in table.Rows)
            {
                if (row.Status != Reviewed) continue;
                string generatedSource = legacy.TryGetValue(sourceLanguage, out var generated) ? generated.Find(r => r.Key == row.Key)?.Value : null;
                if (generatedSource != null && generatedSource != row.Source && generatedSource != row.OriginalSource)
                {
                    row.Status = Review;
                    row.OriginalSource = generatedSource;
                }
                else
                {
                    row.Status = Normal;
                    row.OriginalSource = row.Source;
                }
            }
            var byKey = table.Rows.ToDictionary(r => r.Key, StringComparer.Ordinal);
            // Migration must also preserve entries found only in an old target-language workbook.
            foreach (var oldRows in legacy.Values)
            {
                foreach (var oldRow in oldRows)
                {
                    if (byKey.ContainsKey(oldRow.Key) || sources.Any(s => s.Key == oldRow.Key)) continue;
                    var retained = new TranslationRow { Key = oldRow.Key, Status = Retired,
                        Note = "旧语言表独有条目，请核对原文后保留或清理。", Locked = oldRow.Locked };
                    table.Rows.Add(retained);
                    byKey.Add(retained.Key, retained);
                }
            }
            var sourceKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var source in sources)
            {
                if (string.IsNullOrWhiteSpace(source.Key) || !sourceKeys.Add(source.Key)) throw new InvalidDataException("扫描列表含空或重复 Key。");
                if (!byKey.TryGetValue(source.Key, out var row))
                {
                    row = new TranslationRow { Key = source.Key, Source = source.Value ?? "", OriginalSource = source.Value ?? "" };
                    table.Rows.Add(row);
                    byKey.Add(row.Key, row);
                }
                else if (row.Source != (source.Value ?? ""))
                {
                    row.Source = source.Value ?? "";
                    row.Status = Review;
                }
                row.Locked = source.Locked;
            }
            foreach (var row in table.Rows)
            {
                if (scannedKeys != null)
                {
                    if (!scannedKeys.Contains(row.Key) && !row.Locked && row.Status != Review) row.Status = Retired;
                    else if (row.Status == Retired && !string.IsNullOrWhiteSpace(row.Source)) row.Status = row.Source == row.OriginalSource ? Normal : Review;
                }
                foreach (var language in table.Languages)
                {
                    if (row.Values.ContainsKey(language)) continue;
                    row.Values[language] = legacy.TryGetValue(language, out var oldRows) ? oldRows.Find(r => r.Key == row.Key)?.Value ?? "" : "";
                }
            }
            return table;
        }

        public static Dictionary<Language, List<LocalizationText>> Validate(TranslationTable table,
            Dictionary<Language, List<LocalizationText>> existing)
        {
            var errors = new List<string>();
            var result = table.Languages.ToDictionary(l => l, l => new List<LocalizationText>());
            var lookup = existing.ToDictionary(pair => pair.Key, pair => pair.Value.ToDictionary(r => r.Key, r => r.Value, StringComparer.Ordinal));
            foreach (var row in table.Rows)
            {
                string location = $"第 {row.ExcelRow} 行 [{row.Key}]";
                bool retired = row.Status == Retired;
                if (row.Status == Reviewed && lookup.TryGetValue(table.SourceLanguage, out var sourceLookup) &&
                    sourceLookup.TryGetValue(row.Key, out var generatedSource) && generatedSource != row.Source && generatedSource != row.OriginalSource)
                    errors.Add($"{location} 上次复核后原文再次变化，请重新导出总表，再复核译文。");
                if (!retired && string.IsNullOrWhiteSpace(row.Source)) errors.Add($"{location} 缺少 {table.SourceLanguage} 原文。");
                if ((row.Status == Review || row.Source != row.OriginalSource) && row.Status != Reviewed)
                    errors.Add($"{location} 原文已变化，请复核所有译文后将状态改为“已复核”。");
                foreach (var language in table.Languages)
                {
                    string value = language == table.SourceLanguage ? row.Source : row.Values.TryGetValue(language, out var v) ? v : "";
                    if (string.IsNullOrWhiteSpace(value) && lookup.TryGetValue(language, out var old) && old.TryGetValue(row.Key, out var oldValue)) value = oldValue;
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        if (!retired) errors.Add($"{location} 缺少 {language} 译文，且没有已有译文可保留。");
                        continue;
                    }
                    if (language != table.SourceLanguage && !string.IsNullOrWhiteSpace(row.Source))
                    {
                        string issue = ValidateText(row.Source, value);
                        if (issue != null) errors.Add($"{location} {language}：{issue}");
                    }
                    result[language].Add(new LocalizationText { Key = row.Key, Value = value, Locked = row.Locked });
                }
            }
            if (errors.Count > 0) throw new InvalidDataException($"总表校验失败（{errors.Count} 项），未生成语言文件：\n" + string.Join("\n", errors.Take(100)) + (errors.Count > 100 ? "\n其余错误请修正后继续检查。" : ""));
            return result;
        }

        internal static string ValidateText(string source, string value)
        {
            const string tokens = @"\{[^{}\r\n]+\}|</?[A-Za-z][^>\r\n]*>|%(?:\d+\$)?[-+0 #]*\d*(?:\.\d+)?[a-zA-Z%]|\\[nrt]";
            var sourceTokens = Regex.Matches(source, tokens).Cast<Match>().Select(m => m.Value).OrderBy(s => s, StringComparer.Ordinal);
            var targetTokens = Regex.Matches(value, tokens).Cast<Match>().Select(m => m.Value).OrderBy(s => s, StringComparer.Ordinal);
            if (!sourceTokens.SequenceEqual(targetTokens)) return "占位符、富文本标签或转义符不匹配。";
            if (!TagsBalanced(value)) return "富文本标签未正确闭合。";
            if (Regex.Matches(source, "\r\n|\r|\n").Count != Regex.Matches(value, "\r\n|\r|\n").Count) return "换行数量与原文不一致。";
            if (Regex.Match(source, @"^\s*").Value != Regex.Match(value, @"^\s*").Value ||
                Regex.Match(source, @"\s*$").Value != Regex.Match(value, @"\s*$").Value) return "前后空白与原文不一致。";
            return null;
        }

        internal static byte[] WriteLegacy(List<LocalizationText> rows)
        {
            using (var excel = new ExcelPackage())
            {
                var sheet = excel.Workbook.Worksheets.Add("Sheet1");
                for (int i = 0; i < rows.Count; i++)
                {
                    var checkbox = sheet.Drawings.AddCheckBoxControl(sheet.Cells[i + 1, 1].Address);
                    checkbox.Text = "锁定";
                    checkbox.Checked = rows[i].Locked ? eCheckState.Checked : eCheckState.Unchecked;
                    checkbox.SetSize(30, 20);
                    checkbox.SetPosition(i, 0, 0, 0);
                    sheet.Cells[i + 1, 2].Value = rows[i].Key;
                    sheet.Cells[i + 1, 3].Value = rows[i].Value;
                }
                sheet.Cells.Style.Numberformat.Format = "@";
                sheet.Column(1).Width = 8;
                sheet.Column(2).Width = 45;
                sheet.Column(3).Width = 60;
                return excel.GetAsByteArray();
            }
        }

        // Preflight every destination, then replace with rollback copies. Never publish a partially validated table.
        internal static void Commit(Dictionary<string, byte[]> outputs)
        {
            var staged = new Dictionary<string, string>();
            var backups = new Dictionary<string, string>();
            var published = new List<string>();
            bool keepBackups = false;
            try
            {
                foreach (var pair in outputs)
                {
                    string path = Path.GetFullPath(pair.Key);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    if (File.Exists(path))
                    {
                        using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                    }
                    string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                    staged.Add(path, temporary);
                    File.WriteAllBytes(temporary, pair.Value);
                }
                foreach (var pair in staged)
                {
                    if (File.Exists(pair.Key))
                    {
                        string backup = pair.Value + ".bak";
                        File.Replace(pair.Value, pair.Key, backup);
                        backups.Add(pair.Key, backup);
                    }
                    else File.Move(pair.Value, pair.Key);
                    published.Add(pair.Key);
                }
            }
            catch
            {
                try
                {
                    foreach (string path in published.AsEnumerable().Reverse())
                    {
                        if (backups.TryGetValue(path, out var backup)) File.Copy(backup, path, true);
                        else File.Delete(path);
                    }
                }
                catch (Exception rollbackError)
                {
                    keepBackups = true;
                    throw new IOException("写入失败且回滚受阻；原始文件备份保留在：" + string.Join(", ", backups.Values), rollbackError);
                }
                throw;
            }
            finally
            {
                foreach (string path in staged.Values.Concat(keepBackups ? Enumerable.Empty<string>() : backups.Values))
                    if (File.Exists(path)) File.Delete(path);
            }
        }

        private static bool TagsBalanced(string value)
        {
            var stack = new Stack<string>();
            // Only paired formatting tags; TMP also has standalone tags such as sprite, br, space, pos.
            foreach (Match match in Regex.Matches(value, @"<(/?)(b|i|u|s|color|size|font|material|mark|link|sub|sup|align|voffset|cspace|mspace|nobr|uppercase|lowercase|smallcaps)(?:=[^>]*)?>", RegexOptions.IgnoreCase))
            {
                string name = match.Groups[2].Value.ToLowerInvariant();
                if (match.Groups[1].Value != "/") stack.Push(name);
                else if (stack.Count == 0 || stack.Pop() != name) return false;
            }
            return stack.Count == 0;
        }
    }
}