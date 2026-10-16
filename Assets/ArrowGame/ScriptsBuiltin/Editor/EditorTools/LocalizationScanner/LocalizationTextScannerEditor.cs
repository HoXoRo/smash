using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameFramework.Localization;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    [EditorToolMenu("资源/语言国际化扫描工具", null, 3)]
    public class LocalizationTextScannerEditor : EditorToolBase
    {
        public override string ToolName => "语言国际化工具";
        public override Vector2Int WinSize => new Vector2Int(780, 850);
        private Vector2 textScroll;
        private Vector2 languageScroll;
        private bool showSettings = true;
        private bool showLegacy;
        private bool sourceIsKey;
        private bool dirty;
        private string loadedHash;
        private string filter = "";
        private List<LocalizationText> texts = new List<LocalizationText>();
        private HashSet<string> scannedKeys;

        private void OnEnable()
        {
            TranslationWorkbookService.Imported += OnImported;
            Run(LoadSources);
        }

        private void OnDisable()
        {
            TranslationWorkbookService.Imported -= OnImported;
            EditorToolSettings.Save();
        }

        private void OnImported()
        {
            if (!dirty) Run(LoadSources);
            Repaint();
        }

        private void LoadSources()
        {
            texts = TranslationWorkbookService.LoadSources();
            loadedHash = CurrentHash();
            scannedKeys = null;
            dirty = false;
        }

        private static string CurrentHash() => TranslationWorkbookService.HasWorkbook ?
            TranslationWorkbookService.Hash(TranslationWorkbook.ReadBytes(ConstEditor.TranslationWorkbookPath)) : null;

        private void CheckVersion()
        {
            if (loadedHash != CurrentHash())
                throw new InvalidOperationException("总表已被其他操作修改。请先重新载入总表，再扫描或导出，以免覆盖最新译文。");
        }

        private void OnGUI()
        {
            var settings = EditorToolSettings.Instance;
            EditorGUILayout.HelpBox("① 扫描并补齐原文  →  ② 选择目标语言  →  ③ 导出总表交人工翻译  →  ④ 保存后自动生成", MessageType.Info);
            using (new EditorGUI.DisabledScope(LocalizationTextScanner.IsTranslating))
            {
                EditorGUI.BeginChangeCheck();
                using (new EditorGUI.DisabledScope(TranslationWorkbookService.HasWorkbook))
                {
                    var source = (Language)EditorGUILayout.EnumPopup("主语言", settings.TranslationSourceLanguage);
                    if (source != Language.Unspecified && source != settings.TranslationSourceLanguage)
                    {
                        settings.TranslationSourceLanguage = source;
                        Run(LoadSources);
                    }
                }
                settings.TranslationAutoGenerate = EditorGUILayout.Toggle("总表保存后自动生成", settings.TranslationAutoGenerate);
                if (EditorGUI.EndChangeCheck()) EditorToolSettings.Save();
                EditorGUILayout.LabelField("总表位置", "AAAGameData/Translations/Localization.xlsx");
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("重新载入总表"))
                    {
                        if (!dirty || EditorUtility.DisplayDialog("重新载入", "放弃窗口中尚未导出的原文编辑并载入最新总表？", "重新载入", "取消")) Run(LoadSources);
                    }
                    using (new EditorGUI.DisabledScope(!TranslationWorkbookService.HasWorkbook))
                        if (GUILayout.Button("定位总表")) EditorUtility.RevealInFinder(ConstEditor.TranslationWorkbookPath);
                    if (GUILayout.Button("导入并生成…"))
                    {
                        string path = EditorUtility.OpenFilePanel("选择翻译完成的总表", Path.GetDirectoryName(ConstEditor.TranslationWorkbookPath), "xlsx");
                        if (!string.IsNullOrEmpty(path)) Run(() => TranslationWorkbookService.Import(path));
                    }
                }
                showSettings = EditorGUILayout.Foldout(showSettings, "目标语言（已导出的语言列会保留，避免误删译文）", true);
                if (showSettings) DrawLanguages();
                using (new EditorGUILayout.HorizontalScope())
                {
                    sourceIsKey = EditorGUILayout.ToggleLeft("新增 Key 本身就是原文（仅确认后勾选）", sourceIsKey);
                    if (GUILayout.Button("扫描文本", GUILayout.Width(110))) Run(Scan);
                    if (GUILayout.Button("导出 / 更新总表", GUILayout.Width(150))) Run(Export);
                }
                EditorGUILayout.LabelField($"文本：{texts.Count} 条  |  缺少原文：{texts.Count(t => string.IsNullOrWhiteSpace(t.Value))} 条{(dirty ? "  |  尚未导出" : "")}");
                filter = EditorGUILayout.TextField("筛选 Key / 原文", filter);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("锁定", GUILayout.Width(40));
                    GUILayout.Label("Key（只读）", GUILayout.Width(260));
                    GUILayout.Label(settings.TranslationSourceLanguage + " 原文");
                }
                textScroll = EditorGUILayout.BeginScrollView(textScroll);
                foreach (var text in texts)
                {
                    if (!string.IsNullOrEmpty(filter) && text.Key.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 &&
                        (text.Value ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        text.Locked = EditorGUILayout.Toggle(text.Locked, GUILayout.Width(40));
                        EditorGUILayout.SelectableLabel(text.Key, GUILayout.Width(260), GUILayout.Height(20));
                        text.Value = EditorGUILayout.TextField(text.Value ?? "");
                        if (EditorGUI.EndChangeCheck()) dirty = true;
                    }
                }
                EditorGUILayout.EndScrollView();
                EditorGUILayout.HelpBox(TranslationWorkbookService.LastMessage.Length > 1200 ? TranslationWorkbookService.LastMessage.Substring(0, 1200) + "\n完整信息见 Console。" : TranslationWorkbookService.LastMessage,
                    TranslationWorkbookService.LastFailed ? MessageType.Warning : MessageType.Info);
                showLegacy = EditorGUILayout.Foldout(showLegacy, "旧版分语言表 / GPT 翻译", true);
                if (showLegacy) DrawLegacy();
            }
        }

        private void DrawLanguages()
        {
            var settings = EditorToolSettings.Instance;
            languageScroll = EditorGUILayout.BeginScrollView(languageScroll, GUILayout.MaxHeight(135));
            for (int i = 0; i < settings.LanguagesSupport.Count; i++)
            {
                var language = (Language)settings.LanguagesSupport[i];
                if (language == settings.TranslationSourceLanguage) continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(language.ToString());
                    if (GUILayout.Button("移除选择", GUILayout.Width(80)))
                    {
                        settings.LanguagesSupport.RemoveAt(i--);
                        EditorToolSettings.Save();
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("添加目标语言"))
            {
                var menu = new GenericMenu();
                foreach (Language language in Enum.GetValues(typeof(Language)))
                {
                    if (language == Language.Unspecified || language == settings.TranslationSourceLanguage || settings.LanguagesSupport.Contains((int)language)) continue;
                    Language captured = language;
                    menu.AddItem(new GUIContent(language.ToString()), false, () =>
                    {
                        settings.LanguagesSupport.Add((int)captured);
                        EditorToolSettings.Save();
                        Repaint();
                    });
                }
                menu.ShowAsContext();
            }
        }

        private void Scan()
        {
            CheckVersion();
            var found = LocalizationTextScanner.ScanAllLocalizationText((file, total, index) =>
                EditorUtility.DisplayProgressBar("扫描文本", file, total == 0 ? 0 : index / (float)total));
            scannedKeys = new HashSet<string>(found, StringComparer.Ordinal);
            var existing = new HashSet<string>(texts.Select(t => t.Key), StringComparer.Ordinal);
            int added = 0;
            foreach (string key in found)
            {
                if (!existing.Add(key)) continue;
                texts.Add(new LocalizationText { Key = key, Value = sourceIsKey ? key : "" });
                added++;
            }
            dirty = true;
            TranslationWorkbookService.SetResult($"扫描完成：新增 {added} 条；未扫描到的旧条目将在导出时标为待清理，锁定条目保留。请补齐原文后导出。", false);
        }

        private void Export()
        {
            CheckVersion();
            TranslationWorkbookService.Export(texts, scannedKeys);
            loadedHash = CurrentHash();
            dirty = false;
        }

        private void DrawLegacy()
        {
            EditorGUILayout.HelpBox("此处仅用于迁移前的分语言表流程。已有总表时，请统一在总表维护译文，避免双向覆盖。", MessageType.Info);
            using (new EditorGUI.DisabledScope(TranslationWorkbookService.HasWorkbook))
            {
                var settings = EditorToolSettings.Instance;
                settings.GptTranslationModel = EditorGUILayout.TextField("GPT 模型", settings.GptTranslationModel);
                LocalizationTextScanner.TranslationApiKey = EditorGUILayout.PasswordField("API Key", LocalizationTextScanner.TranslationApiKey);
                settings.GptTranslationMaxLength = EditorGUILayout.IntSlider("每批字符", settings.GptTranslationMaxLength, LocalizationTextScanner.MinLength, LocalizationTextScanner.MaxLength);
                EditorGUILayout.HelpBox("API Key 仅保存在当前 Unity 会话；留空读取 OPENAI_API_KEY 环境变量。", MessageType.None);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("保存分语言表")) Run(() =>
                    {
                        settings.LanguagesSupport = TranslationWorkbookService.SelectedLanguages().Select(l => (int)l).ToList();
                        LocalizationTextScanner.Save2LanguagesExcel(texts);
                    });
                    if (GUILayout.Button("GPT 补齐空译文")) Translate(false);
                    if (GUILayout.Button("GPT 强制重译全部")) Translate(true);
                }
            }
        }

        private static void Translate(bool force)
        {
            if (!EditorUtility.DisplayDialog("GPT 翻译", force ? "使用 API 强制重译所有已有译文？" : "使用 API 翻译分语言表中的空白项？", "开始", "取消")) return;
            EditorToolSettings.Instance.LanguagesSupport = TranslationWorkbookService.SelectedLanguages().Select(l => (int)l).ToList();
            LocalizationTextScanner.TranslateAllLanguages(force, (message, total, index) =>
                EditorUtility.DisplayProgressBar("GPT 翻译", message, total == 0 ? 0 : index / (float)total));
        }

        private static void Run(Action action)
        {
            try { action(); }
            catch (Exception exception) { TranslationWorkbookService.SetResult(exception.Message, true); }
            finally { EditorUtility.ClearProgressBar(); }
        }
    }
}