#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Story.Data;

namespace Story.Editor
{
    /// <summary>
    /// Tools → Story → Generate Word Assets
    /// 10 verb + 10 noun с данными для Intent+Action + Nature системы.
    /// </summary>
    public static class WordAssetGenerator
    {
        private struct WordData
        {
            public string key;
            public string display;
            public WordType type;
            // Intent (verb)
            public string phraseStart;
            public string phrasePast;   // 2-е лицо наст. вр. ("швыряешь")
            public float hpW, powW, sanW;
            // Action (noun)
            public string phraseEnd;
            public int reduction;
            // Sentiment
            public float nature;
        }

        private static readonly WordData[] Words = new[]
        {
            // ── Глаголы (Intent) ─────────────────────────────────────────
            // phraseStart = инфинитив, phrasePast = 2-е лицо наст. вр.
            // nature: > 0 дружелюбно, < 0 агрессивно
            new WordData { key="hurl",      display="Швырнуть",   type=WordType.Verb,
                phraseStart="Швырнуть",   phrasePast="швыряешь",      hpW=3f, powW=1f, sanW=0f, nature=-0.4f },

            new WordData { key="throw",     display="Метнуть",    type=WordType.Verb,
                phraseStart="Метнуть",    phrasePast="метаешь",       hpW=2f, powW=2f, sanW=0f, nature=-0.5f },

            new WordData { key="give",      display="Подарить",   type=WordType.Verb,
                phraseStart="Подарить",   phrasePast="даришь",        hpW=0f, powW=1f, sanW=0f, nature= 0.8f },

            new WordData { key="show",      display="Показать",   type=WordType.Verb,
                phraseStart="Показать",   phrasePast="показываешь",   hpW=0f, powW=0f, sanW=1f, nature= 0.3f },

            new WordData { key="use",       display="Применить",  type=WordType.Verb,
                phraseStart="Применить",  phrasePast="применяешь",    hpW=1f, powW=1f, sanW=1f, nature= 0.5f },

            new WordData { key="hide",      display="Спрятать",   type=WordType.Verb,
                phraseStart="Спрятать",   phrasePast="прячешь",       hpW=0f, powW=1f, sanW=2f, nature= 0.1f },

            new WordData { key="toss",      display="Бросить",    type=WordType.Verb,
                phraseStart="Бросить",    phrasePast="бросаешь",      hpW=1f, powW=2f, sanW=1f, nature=-0.3f },

            new WordData { key="offer",     display="Предложить", type=WordType.Verb,
                phraseStart="Предложить", phrasePast="предлагаешь",   hpW=0f, powW=0f, sanW=1f, nature= 0.7f },

            new WordData { key="grab",      display="Выхватить",  type=WordType.Verb,
                phraseStart="Выхватить",  phrasePast="выхватываешь",  hpW=2f, powW=2f, sanW=0f, nature=-0.3f },

            new WordData { key="raise",     display="Поднять",    type=WordType.Verb,
                phraseStart="Поднять",    phrasePast="поднимаешь",    hpW=1f, powW=1f, sanW=1f, nature= 0.0f },

            // ── Существительные (Action) ─────────────────────────────────
            // phraseEnd = объект в вин. падеже + контекст
            // nature: > 0 мирное/полезное, < 0 агрессивное/опасное
            new WordData { key="sword",     display="Меч",        type=WordType.Noun,
                phraseEnd="верный клинок",           reduction=5, nature=-0.5f },

            new WordData { key="shield",    display="Щит",        type=WordType.Noun,
                phraseEnd="крепкий щит",             reduction=4, nature= 0.2f },

            new WordData { key="torch",     display="Факел",      type=WordType.Noun,
                phraseEnd="горящий факел",           reduction=3, nature= 0.1f },

            new WordData { key="medicine",  display="Лекарство",  type=WordType.Noun,
                phraseEnd="целебное зелье",          reduction=6, nature= 0.9f },

            new WordData { key="map",       display="Карта",      type=WordType.Noun,
                phraseEnd="старую карту",            reduction=4, nature= 0.3f },

            new WordData { key="rations",   display="Провиант",   type=WordType.Noun,
                phraseEnd="горсть провианта",        reduction=3, nature= 0.7f },

            new WordData { key="amulet",    display="Амулет",     type=WordType.Noun,
                phraseEnd="древний амулет",          reduction=4, nature= 0.4f },

            new WordData { key="poison",    display="Яд",         type=WordType.Noun,
                phraseEnd="склянку с ядом",          reduction=5, nature=-0.7f },

            new WordData { key="book",      display="Книга",      type=WordType.Noun,
                phraseEnd="старинный фолиант",       reduction=3, nature= 0.3f },

            new WordData { key="dagger",    display="Кинжал",     type=WordType.Noun,
                phraseEnd="острый кинжал",           reduction=5, nature=-0.7f },
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

                // Intent (verb)
                so.phraseStart = data.phraseStart ?? "";
                so.phrasePast  = data.phrasePast ?? "";
                so.hpWeight    = data.hpW;
                so.powWeight   = data.powW;
                so.sanWeight   = data.sanW;

                // Action (noun)
                so.phraseEnd        = data.phraseEnd ?? "";
                so.penaltyReduction = data.reduction;

                // Nature
                so.nature = data.nature;

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
