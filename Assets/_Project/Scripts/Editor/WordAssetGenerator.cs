#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Story.Data;

namespace Story.Editor
{
    /// <summary>
    /// Tools → Story → Generate Word Assets
    /// 6 подходов (approach) + 6 опор (support) для системы Подход+Опора.
    /// </summary>
    public static class WordAssetGenerator
    {
        private struct WordData
        {
            public string key;
            public string display;
            public WordType type;
            // Approach
            public string approachAdverb;
            public string approachAdverbPast;
            public float hpW, powW, sanW;
            // Support
            public string supportAdverb;
            public int reduction;
            // Sentiment
            public WordArchetype archetype;
            public float chanceModifier;
        }

        private static readonly WordData[] Words = new[]
        {
            // ── Подходы (Approach) ────────────────────────────────────────
            // approachAdverb = наречие на кнопке, approachAdverbPast = для outcome
            // hpW/powW/sanW = куда идёт штраф
            new WordData { key="careful",    display="Осторожно",  type=WordType.Approach,
                approachAdverb="осторожно",  approachAdverbPast="осторожно",
                hpW=1f, powW=0f, sanW=2f, archetype=WordArchetype.Mental, chanceModifier=0.1f },

            new WordData { key="forceful",   display="Напролом",   type=WordType.Approach,
                approachAdverb="напролом",   approachAdverbPast="напролом",
                hpW=3f, powW=2f, sanW=0f, archetype=WordArchetype.Physical, chanceModifier=0.15f },

            new WordData { key="secret",     display="Тайно",      type=WordType.Approach,
                approachAdverb="тайно",      approachAdverbPast="тайно",
                hpW=0f, powW=1f, sanW=2f, archetype=WordArchetype.Social, chanceModifier=0.05f },

            new WordData { key="open",       display="Открыто",    type=WordType.Approach,
                approachAdverb="открыто",    approachAdverbPast="открыто",
                hpW=1f, powW=1f, sanW=1f, archetype=WordArchetype.Social, chanceModifier=0.05f },

            new WordData { key="patient",    display="Терпеливо",  type=WordType.Approach,
                approachAdverb="терпеливо",  approachAdverbPast="терпеливо",
                hpW=0f, powW=0f, sanW=3f, archetype=WordArchetype.Mental, chanceModifier=0.05f },

            new WordData { key="desperate",  display="Отчаянно",   type=WordType.Approach,
                approachAdverb="отчаянно",   approachAdverbPast="отчаянно",
                hpW=3f, powW=3f, sanW=1f, archetype=WordArchetype.Physical, chanceModifier=0.2f },

            // Physical #3: дерзкий вызов — между напролом и отчаянно по риску
            new WordData { key="defiant",    display="Дерзко",     type=WordType.Approach,
                approachAdverb="дерзко",     approachAdverbPast="дерзко",
                hpW=2f, powW=2f, sanW=0f, archetype=WordArchetype.Physical, chanceModifier=0.1f },

            // Mental #3: холодный расчёт без эмоций (убрали «Хитро» — конфликт с опорой «Хитростью»)
            new WordData { key="cold",       display="Холодно",    type=WordType.Approach,
                approachAdverb="холодно",    approachAdverbPast="холодно",
                hpW=0f, powW=1f, sanW=2f, archetype=WordArchetype.Mental, chanceModifier=0.12f },

            // Social #3: безмолвное действие — парадокс мира Феррум Мора
            new WordData { key="silent",     display="Молча",      type=WordType.Approach,
                approachAdverb="молча",      approachAdverbPast="молча",
                hpW=0f, powW=1f, sanW=2f, archetype=WordArchetype.Social, chanceModifier=0.08f },

            // ── Опоры (Support) ───────────────────────────────────────────
            // supportAdverb = наречие на кнопке и в outcome
            // reduction = уменьшение штрафа
            new WordData { key="strength",   display="Силой",      type=WordType.Support,
                supportAdverb="силой",      reduction=3, archetype=WordArchetype.Physical, chanceModifier=0.05f },

            // Physical #2: через боль — высокий риск, самопожертвование
            new WordData { key="pain",       display="Болью",      type=WordType.Support,
                supportAdverb="болью",      reduction=3, archetype=WordArchetype.Physical, chanceModifier=0.08f },

            new WordData { key="luck",       display="Удачей",     type=WordType.Support,
                supportAdverb="удачей",     reduction=2, archetype=WordArchetype.Physical, chanceModifier=0.15f },

            new WordData { key="knowledge",  display="Знанием",    type=WordType.Support,
                supportAdverb="знанием",    reduction=5, archetype=WordArchetype.Mental, chanceModifier=0.1f },

            new WordData { key="cunning",    display="Хитростью",  type=WordType.Support,
                supportAdverb="хитростью",  reduction=4, archetype=WordArchetype.Mental, chanceModifier=0.1f },

            // Mental #3: через память прошлого — нарративная опора
            new WordData { key="memory",     display="Памятью",    type=WordType.Support,
                supportAdverb="памятью",    reduction=4, archetype=WordArchetype.Mental, chanceModifier=0.08f },

            new WordData { key="word",       display="Словом",     type=WordType.Support,
                supportAdverb="словом",     reduction=4, archetype=WordArchetype.Social, chanceModifier=0.1f },

            new WordData { key="gold",       display="Золотом",    type=WordType.Support,
                supportAdverb="золотом",    reduction=6, archetype=WordArchetype.Social, chanceModifier=0.05f },

            // Social #3: через авторитет имени — редкое, только за успех в поздних событиях
            new WordData { key="name",       display="Именем",     type=WordType.Support,
                supportAdverb="именем",     reduction=5, archetype=WordArchetype.Social, chanceModifier=0.12f },
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

                // Approach
                so.approachAdverb     = data.approachAdverb ?? "";
                so.approachAdverbPast = data.approachAdverbPast ?? "";
                so.hpWeight           = data.hpW;
                so.powWeight          = data.powW;
                so.sanWeight          = data.sanW;

                // Support
                so.supportAdverb    = data.supportAdverb ?? "";
                so.penaltyReduction = data.reduction;

                // Archetype & Chance
                so.archetype       = data.archetype;
                so.chanceModifier  = data.chanceModifier;

                if (existing == null)
                    AssetDatabase.CreateAsset(so, path);
                else
                    EditorUtility.SetDirty(so);

                if (db != null) db.words.Add(so);
            }

            if (db != null) EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[WordAssetGenerator] Создано/обновлено {Words.Length} карточек в {folder}");
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
