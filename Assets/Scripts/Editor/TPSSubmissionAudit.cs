#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Static readiness report. It never claims that a gameplay feature passes without a Play Mode test.
/// Makes no changes to scenes, game objects, code or project settings.
/// </summary>
public static class TPSSubmissionAudit
{
    private static readonly Regex LegacyInput = new Regex(
        @"\bInput\s*\.\s*(GetKey|GetKeyDown|GetKeyUp|GetAxis|GetAxisRaw|GetButton|GetButtonDown|GetButtonUp)\s*\(",
        RegexOptions.Compiled);

    [MenuItem("Tools/TPS Shooter/Generate Submission Audit (79-80)")]
    public static void CreateReport()
    {
        var report = new StringBuilder(12000);
        int checks = 0;
        int warnings = 0;
        string root = Directory.GetCurrentDirectory();

        report.AppendLine("TPS SHOOTER — ПРЕДВАРИТЕЛЬНАЯ ПРОВЕРКА ПЕРЕД СДАЧЕЙ");
        report.AppendLine("Создано: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        report.AppendLine("Это проверка наличия ресурсов, НЕ автоматическая оценка в 750 баллов.");
        report.AppendLine(new string('=', 65));

        Action<string, bool> check = (label, ok) =>
        {
            checks++;
            if (!ok) warnings++;
            report.AppendLine((ok ? "[OK] " : "[ПРОВЕРИТЬ] ") + label);
        };

        report.AppendLine("\n1. ПРОЕКТ И СЦЕНЫ");
        check("Версия Unity 2022.3.62f3: " + Application.unityVersion,
            Application.unityVersion.StartsWith("2022.3.62f3", StringComparison.Ordinal));
        check("Рендер-пайплайн назначен (проверь, что это URP)",
            UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null);

        var enabledScenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
        check("Стартовая сцена MainMenu", enabledScenes.Length > 0 &&
            enabledScenes[0].path.EndsWith("MainMenu.unity", StringComparison.OrdinalIgnoreCase));
        check("scene_sandbox присутствует в Build Settings", enabledScenes.Any(s =>
            s.path.EndsWith("scene_sandbox.unity", StringComparison.OrdinalIgnoreCase)));
        check(".gitignore присутствует в корне", File.Exists(Path.Combine(root, ".gitignore")));
        check(".git присутствует в рабочей папке (ветку проверить отдельно)",
            Directory.Exists(Path.Combine(root, ".git")) || File.Exists(Path.Combine(root, ".git")));
        string head = Path.Combine(root, ".git", "HEAD");
        if (File.Exists(head))
        {
            string contents = File.ReadAllText(head).Trim();
            check("Текущая ветка tps_shooter", contents == "ref: refs/heads/tps_shooter");
        }
        else
            report.AppendLine("[РУЧНАЯ ПРОВЕРКА] Ветка git: git branch --show-current");

        report.AppendLine("\n2. ОБЯЗАТЕЛЬНЫЕ СИСТЕМЫ (НАЛИЧИЕ)");
        check("Extenject / Zenject (Assets/Plugins/Zenject)",
            AssetDatabase.IsValidFolder("Assets/Plugins/Zenject"));
        check("ProjectContext prefab", AssetDatabase.FindAssets("ProjectContext t:Prefab").Length > 0);
        check("Input Actions TPSControls", AssetDatabase.FindAssets("TPSControls t:InputActionAsset").Length > 0 ||
            File.Exists("Assets/Input/TPSControls.inputactions"));
        check("Оба типа ИИ и отдельные классы состояний",
            File.Exists("Assets/Scripts/Ai/AiStateMachine.cs") &&
            File.Exists("Assets/Scripts/Ai/AiMeleeAttackState.cs") &&
            File.Exists("Assets/Scripts/Ai/AiAttackTargetState.cs"));
        check("Фабрика врагов", File.Exists("Assets/Scripts/Infrastructure/Zenject/EnemyFactory.cs"));
        check("Фабрика снарядов", File.Exists("Assets/Scripts/Infrastructure/Zenject/ProjectileFactory.cs"));
        check("Фабрика пикапов", File.Exists("Assets/Scripts/Infrastructure/Zenject/PickupFactory.cs"));
        check("Система подбора оружия", File.Exists("Assets/Scripts/Environment/WeaponPickup.cs"));
        check("DOTween присутствует в загруженных сборках", AppDomain.CurrentDomain.GetAssemblies().Any(a =>
            a.GetType("DG.Tweening.DOTween", false) != null));
        check("Основная миссия", File.Exists("Assets/Scripts/Level/TPSMissionDirector.cs"));
        check("Волны противников", File.Exists("Assets/Scripts/Level/TPSWaveSpawner.cs"));
        check("Динамический список целей на экране", File.Exists("Assets/Scripts/UI/TPSMissionGuideHUD.cs"));
        check("Полноэкранная карта на TAB (скрипт)", File.Exists("Assets/Scripts/UI/TPSFullscreenMap.cs"));
        check("Настройки оружия ScriptableObject (скрипт)",
            File.Exists("Assets/Scripts/Weapons/TPSWeaponProfile.cs"));
        check("Найден хотя бы один AudioMixer", AssetDatabase.FindAssets("t:AudioMixer").Length > 0);

        report.AppendLine("\n3. ОБЪЕКТЫ В ТЕКУЩЕЙ ОТКРЫТОЙ СЦЕНЕ");
        report.AppendLine("Сцена: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        check("SceneContext", GameObject.Find("SceneContext") != null);
        check("TPS_ThreeZoneMission", GameObject.Find("TPS_ThreeZoneMission") != null);
        check("EnemyWaveSpawner", GameObject.Find("EnemyWaveSpawner") != null);
        check("TPS_MissionGuideCanvas", GameObject.Find("TPS_MissionGuideCanvas") != null);
        check("TPS_FullscreenMapCanvas", GameObject.Find("TPS_FullscreenMapCanvas") != null);

        report.AppendLine("\n4. ВОЗМОЖНЫЕ ПРЕПЯТСТВИЯ ЗАЧЁТУ: УСТАРЕВШИЙ ВВОД");
        int legacyCount = 0;
        if (Directory.Exists("Assets/Scripts"))
        {
            foreach (string filename in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories))
            {
                if (filename.Replace('\\', '/').Contains("/Editor/")) continue;
                string content = File.ReadAllText(filename);
                if (!LegacyInput.IsMatch(content)) continue;
                legacyCount++;
                report.AppendLine("[ПРОВЕРИТЬ] " + filename.Replace('\\', '/'));
            }
        }
        if (legacyCount == 0)
            report.AppendLine("[OK] Не найдено вызовов Input.GetKey/GetAxis/GetButton в Assets/Scripts.");
        else
            report.AppendLine("Всего файлов с потенциальным legacy input: " + legacyCount);

        report.AppendLine("\n5. ОБЯЗАТЕЛЬНО ПРОВЕРИТЬ В PLAY MODE (НЕ ПРОВЕРЯЕТСЯ СКРИПТОМ)");
        report.AppendLine("[ ] Полный цикл: меню -> игра -> карта -> ключ -> 6 убийств -> E -> вторая зона -> эвакуация -> победа");
        report.AppendLine("[ ] Поражение, пауза, рестарт, настройки звука работают");
        report.AppendLine("[ ] 5 дальнобойных видов + ближний бой отличаются механиками");
        report.AppendLine("[ ] Противники двух типов атакуют, FSM работает и NavMesh запечён");
        report.AppendLine("[ ] Реально используются фабрики Zenject, а не только зарегистрированы");
        report.AppendLine("[ ] Корутины останавливаются при отключении/уничтожении объектов");
        report.AppendLine("[ ] Нет ошибок Console после полного прохождения");
        report.AppendLine("[ ] На чистом клоне есть .dll DOTween/Zenject, .meta и NavMesh.asset");
        report.AppendLine("[ ] README содержит управление, запуск, описание DI, FSM и паттернов");
        report.AppendLine("[ ] Итоговый git status чистый, ветка называется tps_shooter");

        report.AppendLine("\nАвтоматические проверки: " + checks + ", требуют внимания: " + warnings);
        report.AppendLine("Файлы/объекты выше проверены на наличие, не на работоспособность.");
        string destination = Path.Combine(root, "TPS_SUBMISSION_AUDIT.txt");
        File.WriteAllText(destination, report.ToString(), new UTF8Encoding(true));
        Debug.Log("TPS Submission Audit: сохранён отчёт " + destination);
        EditorUtility.DisplayDialog("TPS Shooter",
            "Отчёт TPS_SUBMISSION_AUDIT.txt создан в корне проекта.\n" +
            "Автоматических проверок: " + checks + "\nТребуют внимания: " + warnings +
            "\n\nОтчёт НЕ заменяет Play Mode и проверку чистого клона.", "OK");
        EditorUtility.RevealInFinder(destination);
    }
}
#endif
