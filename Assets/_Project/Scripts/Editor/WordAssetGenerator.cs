#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Story.Data;

namespace Story.Editor
{
    /// <summary>
    /// Tools → Story → Generate Word Assets
    /// 10 adj + 10 noun с данными для Intent+Action системы.
    /// </summary>
    public static class WordAssetGenerator
    {
        private struct WordData
        {
            public string key;
            public string display;
            public WordType type;
            // Intent (adj)
            public string phraseStart;
            public float hpW, powW, sanW;
            // Action (noun)
            public string phraseEnd;
            public int reduction;
        }

        private static readonly WordData[] Words = new[]
        {
            // ── Прилагательные (Intent) ──────────────────────────────────
            new WordData { key="brave",     display="Храбрый",    type=WordType.Adjective,
                phraseStart="Храбро",     hpW=3f, powW=1f, sanW=0f },

            new WordData { key="cunning",   display="Хитрый",     type=WordType.Adjective,
                phraseStart="Хитро",      hpW=0f, powW=1f, sanW=3f },

            new WordData { key="resilient", display="Стойкий",    type=WordType.Adjective,
                phraseStart="Стойко",     hpW=2f, powW=1f, sanW=1f },

            new WordData { key="brutal",    display="Жестокий",   type=WordType.Adjective,
                phraseStart="Жестоко",    hpW=1f, powW=0f, sanW=3f },

            new WordData { key="cautious",  display="Осторожный", type=WordType.Adjective,
                phraseStart="Осторожно",  hpW=1f, powW=1f, sanW=1f },

            new WordData { key="hungry",    display="Голодный",   type=WordType.Adjective,
                phraseStart="Отчаянно",   hpW=0f, powW=3f, sanW=1f },

            new WordData { key="strong",    display="Сильный",    type=WordType.Adjective,
                phraseStart="Мощно",      hpW=1f, powW=3f, sanW=0f },

            new WordData { key="weary",     display="Усталый",    type=WordType.Adjective,
                phraseStart="Вяло",       hpW=1f, powW=1f, sanW=2f },

            new WordData { key="wise",      display="Мудрый",     type=WordType.Adjective,
                phraseStart="Мудро",      hpW=0f, powW=0f, sanW=1f },

            new WordData { key="mad",       display="Безумный",   type=WordType.Adjective,
                phraseStart="Безумно",    hpW=3f, powW=0f, sanW=3f },

            // ── Существительные (Action) ─────────────────────────────────
            new WordData { key="sword",     display="Меч",        type=WordType.Noun,
                phraseEnd="ударить мечом",      reduction=5 },

            new WordData { key="shield",    display="Щит",        type=WordType.Noun,
                phraseEnd="прикрыться щитом",   reduction=4 },

            new WordData { key="torch",     display="Факел",      type=WordType.Noun,
                phraseEnd="осветить факелом",   reduction=3 },

            new WordData { key="medicine",  display="Лекарство",  type=WordType.Noun,
                phraseEnd="применить лекарство", reduction=6 },

            new WordData { key="map",       display="Карта",      type=WordType.Noun,
                phraseEnd="свериться с картой", reduction=4 },

            new WordData { key="rations",   display="Провиант",   type=WordType.Noun,
                phraseEnd="поделиться провиантом", reduction=3 },

            new WordData { key="amulet",    display="Амулет",     type=WordType.Noun,
                phraseEnd="коснуться амулета",  reduction=4 },

            new WordData { key="poison",    display="Яд",         type=WordType.Noun,
                phraseEnd="использовать яд",    reduction=5 },

            new WordData { key="book",      display="Книга",      type=WordType.Noun,
                phraseEnd="прочесть заклинание", reduction=3 },

            new WordData { key="dagger",    display="Кинжал",     type=WordType.Noun,
                phraseEnd="метнуть кинжал",     reduction=5 },
        };

        // ── Меню ────────────────────────────────────────────────────────────

        [MenuItem("Tools/Story/Generate Word Assets")]
        public static void Generate()
        {
            const string folder = "Assets/_Project/Data/Words";
            CreateFolder(folder);

            var dbGuids = AssetDatabase.FindAssets("t:WordDatabaseSO");
            WordDatabaseSO db = dbGuids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<WordDatabaseSO>(
                    AssetDatabase.GUIDToAssetPath(dbGuids[0]))
                : null;

            if (db != null) db.words.Clear();

            foreach (var data in Words)
            {
                string path = $"{folder}/Word_{data.key}.asset";

                var existing = AssetDatabase.LoadAssetAtPath<WordSO>(path);
                var so = existing != null ? existing : ScriptableObject.CreateInstance<WordSO>();

                so.key         = data.key;
                so.displayText = data.display;
                so.type        = data.type;

                // Intent (adj)
                so.phraseStart = data.phraseStart ?? "";
                so.hpWeight    = data.hpW;
                so.powWeight   = data.powW;
                so.sanWeight   = data.sanW;

                // Action (noun)
                so.phraseEnd        = data.phraseEnd ?? "";
                so.penaltyReduction = data.reduction;

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
