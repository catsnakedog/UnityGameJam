using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace GameJam.Data.Editor
{
    public sealed class DataManagerWindow : EditorWindow
    {
        // Registered by the project's Editor assembly, which owns its semantic validators.
        public static Func<CancellationToken, Task<string>> ImportProjectSheets;
        DataImportPlan preview;
        SpreadsheetPreview remotePreview;
        CancellationTokenSource cancellation;
        Vector2 scroll;
        Vector2 tableScroll;
        string message = "Google Sheets에서 데이터를 편집하고 전체 시트 가져오기를 누르세요.";
        string search = "";
        int selectedTable;
        int page;
        bool error;
        bool allowRemovals;
        bool showChanges = true;
        bool busy;
        bool advanced;
        SpreadsheetSource[] configSources = Array.Empty<SpreadsheetSource>();
        const int PageSize = 50;

        [MenuItem("Tools/Data Manager")]
        public static void Open()
        {
            var window = GetWindow<DataManagerWindow>("Data Manager");
            window.minSize = new Vector2(760, 600);
            window.Show();
        }

        void OnDisable() { cancellation?.Cancel(); cancellation?.Dispose(); cancellation = null; }

        void OnGUI()
        {
            var settings = DataToolSettings.instance;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            GUILayout.Label("Data Manager", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("SpreadConfig → 활성 Google Sheets → 내부 CSV / 클래스 / DB 갱신", EditorStyles.miniLabel);
            using (new EditorGUI.DisabledScope(busy || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                EditorGUI.BeginChangeCheck();
                settings.spreadConfigUrl = EditorGUILayout.TextField("SpreadConfig URL (탭 포함)", settings.spreadConfigUrl);
                if (EditorGUI.EndChangeCheck()) { settings.SaveSettings(); configSources = Array.Empty<SpreadsheetSource>(); ClearPreview(); }
                EditorGUILayout.HelpBox("버튼을 누를 때 SpreadConfig의 sheetName / id / gid / enabled를 다시 읽습니다. 파일명과 연결 대상은 SpreadConfig에서 관리하고, 게임 내용은 각 데이터 시트에서 편집하세요.", MessageType.Info);
                using (new EditorGUI.DisabledScope(ImportProjectSheets == null))
                    if (GUILayout.Button("전체 시트 가져오기 · CSV / 클래스 갱신", GUILayout.Height(42))) ImportAllSheets();
                if (ImportProjectSheets == null) EditorGUILayout.HelpBox("프로젝트 가져오기 도구가 컴파일되면 버튼이 활성화됩니다.", MessageType.Info);
                if (GUILayout.Button("Addressables 콘텐츠 빌드")) Run(() => { DataImporter.BuildContent(); message = "Addressables 콘텐츠 빌드 완료."; });
                advanced = EditorGUILayout.Foldout(advanced, "시트 연결 설정 / 고급 미리보기", true);
                if (advanced)
                {
                EditorGUI.BeginChangeCheck();
                settings.csvFolder = EditorGUILayout.TextField("CSV 폴더", settings.csvFolder);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("폴더 선택"))
                    {
                        string path = EditorUtility.OpenFolderPanel("CSV 폴더", Application.dataPath, "");
                        if (!string.IsNullOrEmpty(path))
                        {
                            try { DataImporter.ValidateFolder(path); settings.csvFolder = FileUtil.GetProjectRelativePath(path); settings.SaveSettings(); ClearPreview(); }
                            catch (Exception ex) { SetError(ex); }
                        }
                    }
                    if (GUILayout.Button("폴더 열기")) Run(() => EditorUtility.RevealInFinder(DataImporter.ValidateFolder(settings.csvFolder)));
                    if (GUILayout.Button("사용법")) EditorUtility.RevealInFinder(Path.GetFullPath("Docs/DataManager.md"));
                }
                if (EditorGUI.EndChangeCheck()) { settings.SaveSettings(); ClearPreview(); }
                if (GUILayout.Button("SpreadConfig 연결 확인")) FetchConfig();
                if (configSources.Length > 0) EditorGUILayout.LabelField("마지막 연결 확인 결과", EditorStyles.miniLabel);
                foreach (var source in configSources)
                {
                    EditorGUILayout.LabelField(source.tableName + ".csv", source.enabled ? (source.tableName == "FarmingData" ? "활성 · 파밍 전용" : "활성 · 공용") : "비활성 · 기존 데이터 유지");
                    if (source.enabled) EditorGUILayout.SelectableLabel(source.url, EditorStyles.miniLabel, GUILayout.Height(18));
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("CSV 검증 / 미리보기", GUILayout.Height(30))) Run(() =>
                    {
                        ClearPreview(); preview = DataImporter.Preview(settings.csvFolder); ShowPreviewMessage();
                    });
                    if (GUILayout.Button("공용 시트 가져오기 / 미리보기 (파밍 제외)", GUILayout.Height(30))) FetchSheets();
                }
                using (new EditorGUI.DisabledScope(preview == null))
                {
                    if (preview != null && preview.HasRemovals)
                        allowRemovals = EditorGUILayout.ToggleLeft("미리보기의 행 / 열 / 테이블 삭제 및 스키마 변경 허용", allowRemovals);
                    using (new EditorGUI.DisabledScope(preview != null && preview.HasRemovals && !allowRemovals))
                    {
                        if (GUILayout.Button("미리보기 적용 · DB / C# 갱신", GUILayout.Height(30))) Run(() =>
                        {
                            if (remotePreview != null) SpreadsheetImporter.Apply(remotePreview, allowRemovals);
                            else DataImporter.Apply(preview, allowRemovals);
                            ClearPreview(); message = "DB와 타입 접근 코드를 갱신했습니다. 배포용 데이터는 Addressables 콘텐츠 빌드로 갱신하세요.";
                        });
                    }
                }
                }
            }
            if (busy && GUILayout.Button("다운로드 취소")) cancellation?.Cancel();
            EditorGUILayout.HelpBox(message, error ? MessageType.Error : MessageType.Info);
            if (preview != null) DrawPreview();
            EditorGUILayout.EndScrollView();
        }

        void DrawPreview()
        {
            showChanges = EditorGUILayout.Foldout(showChanges, $"변경 {preview.Changes.Count}건 · SHA {preview.SourceHash.Substring(0, 12)}", true);
            if (showChanges)
            {
                foreach (var change in preview.Changes.Take(200)) EditorGUILayout.LabelField(change, EditorStyles.wordWrappedMiniLabel);
                if (preview.Changes.Count > 200) EditorGUILayout.LabelField("첫 200건 표시. 적용 이력에는 전체 변경이 기록됩니다.");
            }
            selectedTable = Mathf.Clamp(selectedTable, 0, preview.Tables.Count - 1);
            EditorGUI.BeginChangeCheck();
            selectedTable = EditorGUILayout.Popup("테이블", selectedTable, preview.Tables.Select(t => $"{t.Name} ({t.Count})").ToArray());
            search = EditorGUILayout.TextField("ID / 값 검색", search);
            if (EditorGUI.EndChangeCheck()) page = 0;
            var table = preview.Tables[selectedTable];
            var rows = table.Rows.Where(r => string.IsNullOrEmpty(search) || r.Cells.Any(c => c.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
            int pages = Math.Max(1, (rows.Length + PageSize - 1) / PageSize);
            page = Mathf.Clamp(page, 0, pages - 1);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(page == 0)) if (GUILayout.Button("이전", GUILayout.Width(55))) page--;
                GUILayout.Label($"{page + 1} / {pages} 페이지 · {rows.Length}행");
                using (new EditorGUI.DisabledScope(page >= pages - 1)) if (GUILayout.Button("다음", GUILayout.Width(55))) page++;
            }
            tableScroll = EditorGUILayout.BeginScrollView(tableScroll, GUILayout.Height(260));
            using (new EditorGUILayout.HorizontalScope())
                foreach (var column in table.Columns) GUILayout.Label(column.Name + ":" + column.Type, EditorStyles.boldLabel, GUILayout.Width(155));
            foreach (var row in rows.Skip(page * PageSize).Take(PageSize))
                using (new EditorGUILayout.HorizontalScope())
                    foreach (var cell in row.Cells) EditorGUILayout.SelectableLabel(cell, GUILayout.Width(155), GUILayout.Height(20));
            EditorGUILayout.EndScrollView();
        }

        async void FetchSheets()
        {
            ClearPreview(); busy = true; error = false; message = "시트 다운로드 및 검증 중…";
            cancellation?.Dispose(); cancellation = new CancellationTokenSource();
            try
            {
                var settings = DataToolSettings.instance;
                string configUrl = settings.spreadConfigUrl, folder = settings.csvFolder;
                configSources = (await SpreadConfigReader.DownloadAsync(configUrl, cancellation.Token)).ToArray();
                var common = configSources.Where(s => !string.Equals(s.tableName, "FarmingData", StringComparison.OrdinalIgnoreCase)).ToArray();
                remotePreview = await SpreadsheetImporter.DownloadAsync(folder, common, cancellation.Token);
                if (settings.spreadConfigUrl != configUrl || settings.csvFolder != folder) throw new InvalidOperationException("연결 설정이 변경되었습니다. 다시 가져오세요.");
                preview = remotePreview.Plan; ShowPreviewMessage();
            }
            catch (OperationCanceledException) { message = "다운로드를 취소했습니다. 기존 파일은 유지됩니다."; }
            catch (Exception ex) { ClearPreview(); SetError(ex); }
            finally { busy = false; if (this) Repaint(); }
        }
        async void FetchConfig()
        {
            ClearPreview();
            busy = true; error = false; configSources = Array.Empty<SpreadsheetSource>();
            message = "SpreadConfig 연결 목록 확인 중…";
            cancellation?.Dispose(); cancellation = new CancellationTokenSource();
            try
            {
                configSources = (await SpreadConfigReader.DownloadAsync(DataToolSettings.instance.spreadConfigUrl, cancellation.Token)).ToArray();
                message = $"SpreadConfig 확인: 활성 {configSources.Count(s => s.enabled)}개 / 전체 {configSources.Length}개. CSV와 DB는 아직 변경하지 않았습니다.";
            }
            catch (OperationCanceledException) { message = "연결 확인을 취소했습니다."; }
            catch (Exception ex) { SetError(ex); }
            finally { busy = false; if (this) Repaint(); }
        }
        async void ImportAllSheets()
        {
            if (busy || ImportProjectSheets == null) return;
            ClearPreview(); configSources = Array.Empty<SpreadsheetSource>(); busy = true; error = false; message = "SpreadConfig 및 활성 시트 다운로드·검증 중…";
            cancellation?.Dispose(); cancellation = new CancellationTokenSource();
            try { message = await ImportProjectSheets(cancellation.Token); }
            catch (OperationCanceledException) { message = "가져오기를 취소했습니다. 기존 데이터는 유지됩니다."; }
            catch (Exception ex) { SetError(ex); }
            finally { busy = false; if (this) Repaint(); }
        }
        void ShowPreviewMessage() { message = $"검증 완료: {preview.Tables.Count}개 테이블, {preview.Tables.Sum(t => t.Count)}행. 변경을 검토하고 적용하세요."; error = false; }
        void ClearPreview() { preview = null; remotePreview = null; allowRemovals = false; page = 0; }
        void Run(Action action) { try { error = false; action(); } catch (Exception ex) { SetError(ex); } }
        void SetError(Exception ex) { error = true; message = ex.Message; }
    }

    [CustomEditor(typeof(GameDatabase))]
    public sealed class GameDatabaseInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var database = (GameDatabase)target;
            EditorGUILayout.HelpBox("시트에서 생성된 읽기 전용 DB입니다. Google Sheets를 수정하고 Data Manager의 전체 시트 가져오기를 누르세요.", MessageType.Info);
            EditorGUILayout.LabelField("테이블", database.Tables.Count.ToString());
            EditorGUILayout.LabelField("Source SHA", database.SourceHash);
            foreach (var table in database.Tables) EditorGUILayout.LabelField(table.Name, table.Count + " rows");
            if (GUILayout.Button("Data Manager 열기")) DataManagerWindow.Open();
        }
    }
}
