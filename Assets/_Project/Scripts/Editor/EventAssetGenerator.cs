#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Story.Data;

namespace Story.Editor
{
    /// <summary>
    /// Меню Tools → Story → Generate Event Assets
    /// Создаёт 7 тестовых EventSO в Assets/_Project/Data/Events/.
    /// Слова из пула назначаются по ключу — убедитесь что WordAssetGenerator уже запущен.
    /// </summary>
    public static class EventAssetGenerator
    {
        private struct EventData
        {
            public string name;
            public string eventText;
            public string[] eventPoolKeys;     // ключи слов для eventWordPool

            public string labelA;
            public string outcomeA;
            public string[] poolAKeys;
            public int aHp, aPow, aSan;

            public string labelB;
            public string outcomeB;
            public string[] poolBKeys;
            public int bHp, bPow, bSan;
        }

        private static readonly EventData[] Events = new[]
        {
            new EventData
            {
                name = "Event_Stranger",
                eventText = "На дороге стоит [adj] незнакомец. Он смотрит на тебя не мигая.",
                eventPoolKeys = new[] { "cautious", "brave" },

                labelA = "Заговорить",
                outcomeA = "Он оказался добрым человеком. Его [adj] слова дали тебе надежду.",
                poolAKeys = new[] { "wise", "brave" },
                aHp = 5, aPow = 0, aSan = 5,

                labelB = "Обойти стороной",
                outcomeB = "Ты сохранил [adj] осторожность. Может, так и лучше.",
                poolBKeys = new[] { "cautious" },
                bHp = 0, bPow = 0, bSan = 5,
            },

            new EventData
            {
                name = "Event_RuinedHut",
                eventText = "Ты находишь разрушенную хижину. На полу лежит [noun].",
                eventPoolKeys = new[] { "medicine", "torch", "dagger" },

                labelA = "Войти и обыскать",
                outcomeA = "Внутри ты нашёл [noun]. Пригодится в пути.",
                poolAKeys = new[] { "medicine", "torch", "map" },
                aHp = 0, aPow = 5, aSan = 0,

                labelB = "Пройти мимо",
                outcomeB = "Ты не задержался. Разумная [adj] осторожность.",
                poolBKeys = new[] { "cautious" },
                bHp = 0, bPow = 0, bSan = 3,
            },

            new EventData
            {
                name = "Event_HungryChildren",
                eventText = "[adj] дети у дороги просят еды. Их глаза смотрят на тебя с надеждой.",
                eventPoolKeys = new[] { "weary", "hungry" },

                labelA = "Поделиться едой",
                outcomeA = "Ты отдал им часть запасов. [adj] поступок греет душу.",
                poolAKeys = new[] { "brave", "wise" },
                aHp = 0, aPow = -15, aSan = 10,

                labelB = "Извиниться и уйти",
                outcomeB = "У тебя нет ничего лишнего. [adj] решение, но горькое.",
                poolBKeys = new[] { "weary" },
                bHp = 0, bPow = 0, bSan = -10,
            },

            new EventData
            {
                name = "Event_Storm",
                eventText = "Надвигается [adj] буря. Ветер несёт песок и холод.",
                eventPoolKeys = new[] { "brutal", "weary" },

                labelA = "Укрыться в лесу",
                outcomeA = "Ты переждал бурю под деревьями. [noun] помог согреться.",
                poolAKeys = new[] { "torch", "amulet" },
                aHp = -5, aPow = 0, aSan = 5,

                labelB = "Идти сквозь бурю",
                outcomeB = "Ты шёл сквозь стихию. [adj] упорство сохранило тебя.",
                poolBKeys = new[] { "resilient", "strong" },
                bHp = -15, bPow = 10, bSan = -5,
            },

            new EventData
            {
                name = "Event_Merchant",
                eventText = "[adj] торговец предлагает [noun] в обмен на услугу.",
                eventPoolKeys = new[] { "cunning", "brave", "map", "rations" },

                labelA = "Принять сделку",
                outcomeA = "Сделка заключена. Ты получил [noun].",
                poolAKeys = new[] { "rations", "map", "medicine" },
                aHp = 0, aPow = -10, aSan = 5,

                labelB = "Отказаться",
                outcomeB = "Ты не доверяешь незнакомцам. [adj] решение.",
                poolBKeys = new[] { "cautious", "wise" },
                bHp = 0, bPow = 0, bSan = -5,
            },

            new EventData
            {
                name = "Event_WoundedSoldier",
                eventText = "[adj] солдат лежит на обочине. Его [noun] сломан.",
                eventPoolKeys = new[] { "weary", "strong", "sword", "shield" },

                labelA = "Перевязать раны",
                outcomeA = "Солдат благодарен. Он отдаёт тебе свой [noun].",
                poolAKeys = new[] { "dagger", "shield", "medicine" },
                aHp = -5, aPow = 0, aSan = 10,

                labelB = "Забрать его вещи",
                outcomeB = "Ты взял [noun]. Он смотрит тебе вслед.",
                poolBKeys = new[] { "sword", "dagger" },
                bHp = 0, bPow = 10, bSan = -15,
            },

            new EventData
            {
                name = "Event_AbandonedLibrary",
                eventText = "Ты находишь [adj] библиотеку. Полки ломятся от [noun].",
                eventPoolKeys = new[] { "weary", "wise", "book", "torch" },

                labelA = "Читать до рассвета",
                outcomeA = "Слова наполняют разум. Ты берёшь с собой [noun].",
                poolAKeys = new[] { "book", "map" },
                aHp = 0, aPow = -5, aSan = 15,

                labelB = "Осмотреться и уйти",
                outcomeB = "Ты взял что нашёл под рукой — [noun].",
                poolBKeys = new[] { "torch", "book" },
                bHp = 0, bPow = 0, bSan = 5,
            },
        };

        [MenuItem("Tools/Story/Generate Event Assets")]
        public static void Generate()
        {
            const string folder = "Assets/_Project/Data/Events";
            CreateFolder(folder);

            // Загружаем все WordSO по ключам
            var wordMap = BuildWordMap();

            foreach (var data in Events)
            {
                string path = $"{folder}/{data.name}.asset";
                var so = AssetDatabase.LoadAssetAtPath<EventSO>(path)
                         ?? ScriptableObject.CreateInstance<EventSO>();

                so.eventText = data.eventText;
                so.weight    = 1f;

                so.eventWordPool = ResolveWords(data.eventPoolKeys, wordMap);

                so.choiceA = BuildChoice(data.labelA, data.outcomeA, data.poolAKeys,
                                         data.aHp, data.aPow, data.aSan, wordMap);
                so.choiceB = BuildChoice(data.labelB, data.outcomeB, data.poolBKeys,
                                         data.bHp, data.bPow, data.bSan, wordMap);

                if (!AssetDatabase.Contains(so))
                    AssetDatabase.CreateAsset(so, path);
                else
                    EditorUtility.SetDirty(so);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // ── Заполнить EventDatabaseSO ─────────────────────────────────
            const string dbPath = "Assets/_Project/Data/SO/EventDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<EventDatabaseSO>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<EventDatabaseSO>();
                AssetDatabase.CreateAsset(db, dbPath);
            }

            db.events.Clear();
            foreach (var data in Events)
            {
                var ev = AssetDatabase.LoadAssetAtPath<EventSO>(
                    $"Assets/_Project/Data/Events/{data.name}.asset");
                if (ev != null) db.events.Add(ev);
            }
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();

            Debug.Log($"[EventAssetGenerator] Создано {Events.Length} ивентов + EventDatabase в {dbPath}");
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static EventChoice BuildChoice(
            string label, string outcome, string[] poolKeys,
            int hp, int pow, int san,
            Dictionary<string, WordSO> wordMap)
        {
            return new EventChoice
            {
                label           = label,
                outcomeText     = outcome,
                rewardWordPool  = ResolveWords(poolKeys, wordMap),
                healthDelta     = hp,
                powerDelta      = pow,
                sanityDelta     = san,
            };
        }

        private static List<WordSO> ResolveWords(string[] keys, Dictionary<string, WordSO> map)
        {
            var list = new List<WordSO>();
            if (keys == null) return list;
            foreach (var k in keys)
                if (map.TryGetValue(k, out var w) && w != null)
                    list.Add(w);
            return list;
        }

        private static Dictionary<string, WordSO> BuildWordMap()
        {
            var map = new Dictionary<string, WordSO>();
            var guids = AssetDatabase.FindAssets("t:WordSO");
            foreach (var g in guids)
            {
                var so = AssetDatabase.LoadAssetAtPath<WordSO>(AssetDatabase.GUIDToAssetPath(g));
                if (so != null && !string.IsNullOrEmpty(so.key))
                    map[so.key] = so;
            }
            return map;
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
