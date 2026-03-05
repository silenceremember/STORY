#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Story.Data;

namespace Story.Editor
{
    /// <summary>
    /// Меню Tools → Story → Generate Word Assets
    /// Создаёт 10 прилагательных + 10 существительных WordSO в Assets/_Project/Data/Words/
    /// и заполняет указанный WordDatabaseSO.
    /// </summary>
    public static class WordAssetGenerator
    {
        // ── Данные слов ──────────────────────────────────────────────────────

        private struct WordData
        {
            public string key;
            public string display;
            public WordType type;
            // active
            public int aHp; public int aPow; public int aSan;
            public string aDesc;
        }

        private static readonly WordData[] Words = new[]
        {
            // ── Прилагательные ────────────────────────────────────────────
            new WordData { key="brave",     display="Храбрый",    type=WordType.Adjective,
                aHp=5,   aPow=0,  aSan=0,
                aDesc="Храбрость восстанавливает силы." },

            new WordData { key="cunning",   display="Хитрый",     type=WordType.Adjective,
                aHp=0,   aPow=0,  aSan=6,
                aDesc="Хитрость проясняет разум." },

            new WordData { key="resilient", display="Стойкий",    type=WordType.Adjective,
                aHp=4,   aPow=0,  aSan=0,
                aDesc="Стойкость — щит от боли." },

            new WordData { key="brutal",    display="Жестокий",   type=WordType.Adjective,
                aHp=10,  aPow=0,  aSan=-3,
                aDesc="Жестокость даёт силу, но ломает разум." },

            new WordData { key="cautious",  display="Осторожный", type=WordType.Adjective,
                aHp=0,   aPow=3,  aSan=3,
                aDesc="Осторожность сберегает ресурсы." },

            new WordData { key="hungry",    display="Голодный",   type=WordType.Adjective,
                aHp=0,   aPow=8,  aSan=0,
                aDesc="Голод превращается в отчаянную энергию." },

            new WordData { key="strong",    display="Сильный",    type=WordType.Adjective,
                aHp=0,   aPow=6,  aSan=0,
                aDesc="Сила открывает новые возможности." },

            new WordData { key="weary",     display="Усталый",    type=WordType.Adjective,
                aHp=0,   aPow=0,  aSan=5,
                aDesc="Усталость отступает перед отдыхом." },

            new WordData { key="wise",      display="Мудрый",     type=WordType.Adjective,
                aHp=0,   aPow=0,  aSan=8,
                aDesc="Мудрость — лучшее лекарство." },

            new WordData { key="mad",       display="Безумный",   type=WordType.Adjective,
                aHp=12,  aPow=0,  aSan=-5,
                aDesc="Безумие пробуждает скрытую ярость." },

            // ── Существительные ───────────────────────────────────────────
            new WordData { key="sword",     display="Меч",        type=WordType.Noun,
                aHp=8,   aPow=0,  aSan=0,
                aDesc="Клинок разрезает путь вперёд." },

            new WordData { key="shield",    display="Щит",        type=WordType.Noun,
                aHp=5,   aPow=0,  aSan=0,
                aDesc="Щит принимает удар на себя." },

            new WordData { key="torch",     display="Факел",      type=WordType.Noun,
                aHp=0,   aPow=0,  aSan=6,
                aDesc="Свет разгоняет тьму внутри." },

            new WordData { key="medicine",  display="Лекарство",  type=WordType.Noun,
                aHp=10,  aPow=0,  aSan=0,
                aDesc="Снадобье возвращает к жизни." },

            new WordData { key="map",       display="Карта",      type=WordType.Noun,
                aHp=0,   aPow=4,  aSan=3,
                aDesc="Знание пути экономит силы." },

            new WordData { key="rations",   display="Провиант",   type=WordType.Noun,
                aHp=0,   aPow=8,  aSan=0,
                aDesc="Сытость восполняет силу." },

            new WordData { key="amulet",    display="Амулет",     type=WordType.Noun,
                aHp=0,   aPow=0,  aSan=7,
                aDesc="Талисман успокаивает разум." },

            new WordData { key="poison",    display="Яд",         type=WordType.Noun,
                aHp=-5,  aPow=0,  aSan=10,
                aDesc="Яд проясняет восприятие — на грани." },

            new WordData { key="book",      display="Книга",      type=WordType.Noun,
                aHp=0,   aPow=0,  aSan=8,
                aDesc="Слова на страницах укрепляют дух." },

            new WordData { key="dagger",    display="Кинжал",     type=WordType.Noun,
                aHp=6,   aPow=3,  aSan=0,
                aDesc="Лёгкий клинок — быстрая помощь." },
        };

        // ── Меню ────────────────────────────────────────────────────────────

        [MenuItem("Tools/Story/Generate Word Assets")]
        public static void Generate()
        {
            const string folder = "Assets/_Project/Data/Words";
            CreateFolder(folder);

            // Ищем WordDatabaseSO в проекте
            var dbGuids = AssetDatabase.FindAssets("t:WordDatabaseSO");
            WordDatabaseSO db = dbGuids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<WordDatabaseSO>(
                    AssetDatabase.GUIDToAssetPath(dbGuids[0]))
                : null;

            if (db != null) db.words.Clear();

            foreach (var data in Words)
            {
                string path = $"{folder}/Word_{data.key}.asset";

                // Не перезаписываем существующий
                var existing = AssetDatabase.LoadAssetAtPath<WordSO>(path);
                var so = existing != null ? existing : ScriptableObject.CreateInstance<WordSO>();

                so.key               = data.key;
                so.displayText       = data.display;
                so.type              = data.type;
                so.activeHealthBonus   = data.aHp;
                so.activePowerBonus    = data.aPow;
                so.activeSanityBonus   = data.aSan;
                so.activeDescription   = data.aDesc;

                if (existing == null)
                    AssetDatabase.CreateAsset(so, path);
                else
                    EditorUtility.SetDirty(so);

                if (db != null) db.words.Add(so);
            }

            if (db != null) EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[WordAssetGenerator] Создано/обновлено {Words.Length} слов в {folder}");
        }

        private static void CreateFolder(string path)
        {
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
