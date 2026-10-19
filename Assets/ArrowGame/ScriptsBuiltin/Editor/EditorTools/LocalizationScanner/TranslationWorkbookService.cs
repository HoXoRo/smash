using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using GameFramework.Localization;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    [InitializeOnLoad]
    internal static class TranslationWorkbookService
    {
        private static readonly string StatePath = Path.GetFullPath("Library/LocalizationTranslation.hash");
        private static double nextPoll;
        private static double changedAt;
        private static string observedStamp;
        private static string attemptedHash;
        private static int retries;
        private static bool busy;
        public static string LastMessage { get; private set; } = "尚未导入翻译总表。";
        public static bool LastFailed { get; private set; }
        public static bool HasWorkbook => File.Exists(ConstEditor.TranslationWorkbookPath);
        public static event Action Imported;

        static TranslationWorkbookService()
        {
            EditorApplication.update += Poll;
        }

        public static List<Language> SelectedLanguages()
        {
            var settings = EditorToolSettings.Instance;
            return new[] { settings.TranslationSourceLanguage }
                .Concat(settings.LanguagesSupport.Select(v => (Language)v))
                .Where(l => l != Language.Unspecified && Enum.IsDefined(typeof(Language), l)).Distinct().ToList();
        }

        public static List<LocalizationText> LoadSources()
        {
            if (HasWorkbook)
            {
                var table = TranslationWorkbook.Read(TranslationWorkbook.ReadBytes(ConstEditor.TranslationWorkbookPath));
                EditorToolSettings.Instance.TranslationSourceLanguage = table.SourceLanguage;
                foreach (var language in table.Languages)
                    if (!EditorToolSettings.Instance.LanguagesSupport.Contains((int)language))
                        EditorToolSettings.Instance.LanguagesSupport.Add((int)language);
                return table.Rows.Select(r => new LocalizationText { Key = r.Key, Value = r.Source, Locked = r.Locked }).ToList();
            }
            return TranslationWorkbook.ReadLegacy(LocalizationTextScanner.GetLanguageExcelFileName(EditorToolSettings.Instance.TranslationSourceLanguage));
        }

        private static Dictionary<Language, List<LocalizationText>> LoadLegacy(IEnumerable<Language> languages)
        {
            return languages.ToDictionary(l => l, l => TranslationWorkbook.ReadLegacy(LocalizationTextScanner.GetLanguageExcelFileName(l)));
        }

        public static void Export(List<LocalizationText> sources, HashSet<string> scannedKeys)
        {
            if (sources.Count == 0) throw new InvalidDataException("没有可导出的文本，请先扫描。");
            var previous = HasWorkbook ? TranslationWorkbook.Read(TranslationWorkbook.ReadBytes(ConstEditor.TranslationWorkbookPath)) : null;
            var languages = SelectedLanguages();
            var allLanguages = languages.Concat(previous?.Languages ?? new List<Language>()).Distinct().ToArray();
            var table = TranslationWorkbook.Merge(sources, EditorToolSettings.Instance.TranslationSourceLanguage,
                languages, previous, LoadLegacy(allLanguages), scannedKeys);
            byte[] bytes = TranslationWorkbook.Write(table);
            TranslationWorkbook.Commit(new Dictionary<string, byte[]> { [ConstEditor.TranslationWorkbookPath] = bytes });
            // An export is a template, not a completed translation. Only an ensuing save/import should generate.
            attemptedHash = Fingerprint(bytes);
            SetResult($"已导出 {table.Rows.Count} 条文本、{table.Languages.Count} 种语言。请填写总表后保存。", false);
            EditorToolSettings.Save();
        }

        public static void Import(string path)
        {
            if (busy || LocalizationTextScanner.IsTranslating) throw new InvalidOperationException("另一个语言任务正在处理，请稍后重试。");
            busy = true;
            try
            {
                byte[] bytes = TranslationWorkbook.ReadBytes(path);
                string inputHash = Hash(bytes);
                var table = TranslationWorkbook.Read(bytes);
                // Imported copies must correspond to the latest exported master. Stale copies cannot erase new keys/languages.
                if (HasWorkbook && !SamePath(path, ConstEditor.TranslationWorkbookPath))
                {
                    var current = TranslationWorkbook.Read(TranslationWorkbook.ReadBytes(ConstEditor.TranslationWorkbookPath));
                    if (current.Revision != table.Revision || current.SourceLanguage != table.SourceLanguage ||
                        !new HashSet<string>(current.Rows.Select(r => r.Key)).SetEquals(table.Rows.Select(r => r.Key)) ||
                        !new HashSet<Language>(current.Languages).SetEquals(table.Languages) ||
                        current.Rows.Any(r => table.Rows.Find(t => t.Key == r.Key)?.OriginalSource != r.OriginalSource))
                        throw new InvalidDataException("导入文件与当前总表版本不一致（Key、原文版本或语言列有变化），请将译文合并到最新总表后再导入。");
                }
                var validated = TranslationWorkbook.Validate(table, LoadLegacy(table.Languages));
                var config = AppConfigs.GetInstanceEditor();
                if (config == null) throw new InvalidOperationException("找不到 AppConfigs，无法确定运行时语言文件格式。");
                var outputs = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
                string stage = Path.GetFullPath(Path.Combine("Library", "TranslationGeneration", Guid.NewGuid().ToString("N")));
                Directory.CreateDirectory(stage);
                try
                {
                    foreach (var pair in validated)
                    {
                        outputs.Add(LocalizationTextScanner.GetLanguageExcelFileName(pair.Key), TranslationWorkbook.WriteLegacy(pair.Value));
                        string json = Path.Combine(stage, pair.Key + ".json");
                        GameDataGenerator.WriteLanguageData(pair.Value, json, config.LoadFromBytes);
                        outputs.Add(Path.GetFullPath(Path.Combine(ConstEditor.LanguagePath, pair.Key + ".json")), File.ReadAllBytes(json));
                        if (config.LoadFromBytes)
                            outputs.Add(Path.GetFullPath(Path.Combine(ConstEditor.LanguagePath, pair.Key + ".bytes")), File.ReadAllBytes(Path.ChangeExtension(json, ".bytes")));
                    }
                    // Keep the workbook open in Excel: never rewrite a user's active workbook on save.
                    // An external delivery is copied into the canonical location only after validation.
                    if (!SamePath(path, ConstEditor.TranslationWorkbookPath))
                        outputs.Add(ConstEditor.TranslationWorkbookPath, bytes);
                    if (Hash(TranslationWorkbook.ReadBytes(path)) != inputHash)
                        throw new IOException("导入期间表格又被保存，请等待保存完成后重试。");
                    TranslationWorkbook.Commit(outputs);
                    // Generated workbooks already have matching runtime files; avoid the old watcher regenerating them.
                    DataTableUpdater.MarkLanguagesGenerated(validated.Keys.Select(LocalizationTextScanner.GetLanguageExcelFileName));
                    AddLanguagesToConfig(config, table.Languages);
                    var settings = EditorToolSettings.Instance;
                    settings.TranslationSourceLanguage = table.SourceLanguage;
                    settings.LanguagesSupport = new[] { (int)table.SourceLanguage }.Concat(table.Languages.Select(l => (int)l)).Distinct().ToList();
                    EditorToolSettings.Save();
                    attemptedHash = Fingerprint(bytes);
                    Directory.CreateDirectory(Path.GetDirectoryName(StatePath));
                    File.WriteAllText(StatePath, attemptedHash);
                    AssetDatabase.Refresh();
                    SetResult($"生成完成：{table.Rows.Count} 条文本，{table.Languages.Count} 种语言（JSON{(config.LoadFromBytes ? " + bytes" : "")}）。", false);
                    Imported?.Invoke();
                }
                finally
                {
                    if (Directory.Exists(stage)) Directory.Delete(stage, true);
                }
            }
            finally { busy = false; }
        }

        private static void AddLanguagesToConfig(AppConfigs config, IEnumerable<Language> languages)
        {
            var serialized = new SerializedObject(config);
            var list = serialized.FindProperty("mLanguages");
            var names = new HashSet<string>(config.Languages ?? Array.Empty<string>());
            var added = new List<string>();
            foreach (var language in languages)
            {
                string name = language.ToString();
                if (!names.Add(name)) continue;
                int index = list.arraySize++;
                list.GetArrayElementAtIndex(index).stringValue = name;
                added.Add(name);
            }
            if (added.Count == 0) return;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssetIfDirty(config);
            Debug.LogWarning("已加入 AppConfigs 语言加载列表：" + string.Join(", ", added) + "。请同时检查 ArrowLanguagesTable 中的语言选择项及对应字体。");
        }

        private static void Poll()
        {
            if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || LocalizationTextScanner.IsTranslating) return;
            double now = EditorApplication.timeSinceStartup;
            if (now < nextPoll) return;
            nextPoll = now + 1;
            if (!EditorToolSettings.Instance.TranslationAutoGenerate || !HasWorkbook) return;
            try
            {
                var info = new FileInfo(ConstEditor.TranslationWorkbookPath);
                string stamp = info.LastWriteTimeUtc.Ticks + ":" + info.Length + ":" + AppConfigs.GetInstanceEditor()?.LoadFromBytes;
                if (stamp != observedStamp)
                {
                    observedStamp = stamp;
                    changedAt = now;
                    retries = 0;
                    return;
                }
                if (now - changedAt < 2) return;
                byte[] bytes = TranslationWorkbook.ReadBytes(info.FullName);
                string fingerprint = Fingerprint(bytes);
                if (fingerprint == attemptedHash || (File.Exists(StatePath) && File.ReadAllText(StatePath) == fingerprint)) return;
                try
                {
                    Import(info.FullName);
                }
                catch (IOException) when (++retries < 5)
                {
                    changedAt = now;
                    return;
                }
                catch (Exception exception)
                {
                    attemptedHash = fingerprint;
                    SetResult(exception.Message, true, warningOnly: true);
                }
            }
            catch (Exception exception)
            {
                // A replaced or temporarily locked workbook is retried, without logging on every editor update.
                if (++retries == 5) SetResult("读取总表失败，将在文件可用后重试：" + exception.Message, true, warningOnly: true);
                changedAt = now;
            }
        }

        public static void SetResult(string message, bool failed, bool warningOnly = false)
        {
            LastMessage = message;
            LastFailed = failed;
            if (failed && warningOnly) Debug.LogWarning(message);
            else if (failed) Debug.LogError(message);
            else Debug.Log(message);
        }

        private static bool SamePath(string a, string b) => string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
        private static string Fingerprint(byte[] bytes) => Hash(bytes) + ":" + AppConfigs.GetInstanceEditor()?.LoadFromBytes;
        internal static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(bytes));
        }
    }
}