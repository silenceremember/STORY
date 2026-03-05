#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Story.Data;

namespace Story.Editor
{
    /// <summary>
    /// Tools → Story → Generate Event Assets
    /// 20 событий под систему Intent+Action.
    /// </summary>
    public static class EventAssetGenerator
    {
        private struct EventData
        {
            public string key;
            public string eventText;
            public int    penalty;
            public string defStart;   // default intent phrase start
            public float  defHp, defPow, defSan;  // default intent weights
            public string defEnd;     // default action phrase end
            public int    defReduction;
            public string outcome;
            public string[] rewardKeys;
            public string[] poolKeys;
            public float  weight;
        }

        // Все ключи слов для быстрого поиска WordSO
        private static readonly string[] AllWordKeys = {
            "brave","cunning","resilient","brutal","cautious","hungry","strong","weary","wise","mad",
            "sword","shield","torch","medicine","map","rations","amulet","poison","book","dagger"
        };

        private static readonly EventData[] Events = new[]
        {
            new EventData {
                key       = "Bandits",
                eventText = "Разбойники перегородили тропу. Главарь ухмыляется, поигрывая ножом.",
                penalty   = -12,
                defStart  = "Осторожно", defHp = 1, defPow = 1, defSan = 1,
                defEnd    = "отступить", defReduction = 0,
                outcome   = "Разбойники отступают. Ты находишь [noun] у дороги.",
                rewardKeys = new[] { "dagger", "shield" },
                poolKeys   = new[] { "sword", "dagger", "shield" },
                weight    = 1f
            },
            new EventData {
                key       = "DarkCave",
                eventText = "Тёмная пещера зияет перед тобой. Изнутри доносится капель и далёкий рык.",
                penalty   = -14,
                defStart  = "Молча", defHp = 1, defPow = 2, defSan = 1,
                defEnd    = "войти", defReduction = 0,
                outcome   = "В глубине пещеры мерцает [noun]. Ты берёшь находку.",
                rewardKeys = new[] { "torch", "amulet" },
                poolKeys   = new[] { "torch", "map", "amulet" },
                weight    = 1f
            },
            new EventData {
                key       = "WoundedMonk",
                eventText = "У обочины лежит раненый монах. Он протягивает руку и шепчет просьбу.",
                penalty   = -10,
                defStart  = "Молча", defHp = 2, defPow = 1, defSan = 0,
                defEnd    = "помочь", defReduction = 0,
                outcome   = "Монах благодарит и оставляет тебе [noun].",
                rewardKeys = new[] { "medicine", "book" },
                poolKeys   = new[] { "medicine", "book", "amulet" },
                weight    = 1f
            },
            new EventData {
                key       = "StoneGolem",
                eventText = "Каменный голем преграждает мост. Его глаза пылают алым.",
                penalty   = -18,
                defStart  = "Осторожно", defHp = 2, defPow = 2, defSan = 1,
                defEnd    = "обойти",   defReduction = 0,
                outcome   = "Голем рассыпается. Среди обломков — [noun].",
                rewardKeys = new[] { "shield", "sword" },
                poolKeys   = new[] { "sword", "shield", "dagger" },
                weight    = 0.8f
            },
            new EventData {
                key       = "ForestSpirits",
                eventText = "Лесные духи танцуют в лунном свете. Они манят тебя присоединиться.",
                penalty   = -11,
                defStart  = "Тихо", defHp = 0, defPow = 1, defSan = 3,
                defEnd    = "наблюдать", defReduction = 0,
                outcome   = "Духи оставляют [adj] подарок — [noun].",
                rewardKeys = new[] { "amulet", "wise" },
                poolKeys   = new[] { "amulet", "book", "torch" },
                weight    = 1f
            },
            new EventData {
                key       = "PoisonedWell",
                eventText = "Колодец у деревни отравлен. Жители смотрят с мольбой.",
                penalty   = -13,
                defStart  = "Молча", defHp = 1, defPow = 2, defSan = 1,
                defEnd    = "пройти мимо", defReduction = 0,
                outcome   = "Ты очищаешь колодец. Староста дарит тебе [noun].",
                rewardKeys = new[] { "medicine", "rations" },
                poolKeys   = new[] { "medicine", "poison", "rations" },
                weight    = 1f
            },
            new EventData {
                key       = "Merchant",
                eventText = "Бродячий торговец раскладывает товары. Цены кусаются.",
                penalty   = -8,
                defStart  = "Молча", defHp = 1, defPow = 1, defSan = 0,
                defEnd    = "торговать", defReduction = 0,
                outcome   = "Торговец уходит, оставив [noun] в подарок.",
                rewardKeys = new[] { "map", "rations" },
                poolKeys   = new[] { "map", "rations", "dagger" },
                weight    = 1.2f
            },
            new EventData {
                key       = "Nightmare",
                eventText = "Ночной кошмар не отпускает. Тени сгущаются, шепот нарастает.",
                penalty   = -15,
                defStart  = "Тихо", defHp = 0, defPow = 1, defSan = 3,
                defEnd    = "перетерпеть", defReduction = 0,
                outcome   = "Рассвет приносит облегчение. Рядом — [noun].",
                rewardKeys = new[] { "book", "amulet" },
                poolKeys   = new[] { "book", "amulet", "torch" },
                weight    = 1f
            },
            new EventData {
                key       = "BridgeTroll",
                eventText = "Тролль под мостом требует плату за проход. Мост — единственный путь.",
                penalty   = -14,
                defStart  = "Молча", defHp = 1, defPow = 2, defSan = 1,
                defEnd    = "заплатить", defReduction = 0,
                outcome   = "Тролль хохочет и швыряет тебе [noun].",
                rewardKeys = new[] { "dagger", "poison" },
                poolKeys   = new[] { "sword", "dagger", "poison" },
                weight    = 0.9f
            },
            new EventData {
                key       = "Ruins",
                eventText = "Древние руины скрывают забытые знания. Стены покрыты письменами.",
                penalty   = -10,
                defStart  = "Тихо", defHp = 0, defPow = 1, defSan = 2,
                defEnd    = "изучать", defReduction = 0,
                outcome   = "Среди камней ты находишь [noun].",
                rewardKeys = new[] { "book", "map" },
                poolKeys   = new[] { "book", "map", "amulet" },
                weight    = 1f
            },
            new EventData {
                key       = "Ambush",
                eventText = "Засада! Стрелы свистят над головой. Укрытие — за ближайшим валуном.",
                penalty   = -16,
                defStart  = "Рефлексивно", defHp = 2, defPow = 2, defSan = 1,
                defEnd    = "пригнуться", defReduction = 0,
                outcome   = "Нападавшие бегут. Ты подбираешь [noun].",
                rewardKeys = new[] { "shield", "sword" },
                poolKeys   = new[] { "shield", "sword", "dagger" },
                weight    = 0.9f
            },
            new EventData {
                key       = "SickVillage",
                eventText = "Деревня охвачена мором. Больные лежат на улицах.",
                penalty   = -12,
                defStart  = "Молча", defHp = 2, defPow = 1, defSan = 1,
                defEnd    = "пройти мимо", defReduction = 0,
                outcome   = "Знахарка благодарит и даёт [noun].",
                rewardKeys = new[] { "medicine", "rations" },
                poolKeys   = new[] { "medicine", "rations", "poison" },
                weight    = 1f
            },
            new EventData {
                key       = "DesertedCamp",
                eventText = "Покинутый лагерь. Костёр ещё тёплый, вещи разбросаны.",
                penalty   = -9,
                defStart  = "Молча", defHp = 1, defPow = 1, defSan = 1,
                defEnd    = "обыскать вещи", defReduction = 0,
                outcome   = "В палатке ты находишь [noun].",
                rewardKeys = new[] { "rations", "torch" },
                poolKeys   = new[] { "rations", "torch", "map" },
                weight    = 1.1f
            },
            new EventData {
                key       = "Portal",
                eventText = "Мерцающий портал висит в воздухе. Из него веет холодом иного мира.",
                penalty   = -17,
                defStart  = "Молча", defHp = 1, defPow = 1, defSan = 3,
                defEnd    = "отвернуться", defReduction = 0,
                outcome   = "Портал схлопывается, выбросив [noun].",
                rewardKeys = new[] { "amulet", "book" },
                poolKeys   = new[] { "amulet", "book", "poison" },
                weight    = 0.7f
            },
            new EventData {
                key       = "HungryWolves",
                eventText = "Стая голодных волков окружает тебя. Вожак скалит зубы.",
                penalty   = -15,
                defStart  = "Медленно", defHp = 2, defPow = 2, defSan = 1,
                defEnd    = "отступить", defReduction = 0,
                outcome   = "Волки убегают. У логова — [noun].",
                rewardKeys = new[] { "dagger", "rations" },
                poolKeys   = new[] { "sword", "dagger", "rations" },
                weight    = 1f
            },
            new EventData {
                key       = "AbandonedCart",
                eventText = "Телега стоит поперёк дороги. Лошади нет, борта изломаны.",
                penalty   = -7,
                defStart  = "Молча", defHp = 1, defPow = 1, defSan = 0,
                defEnd    = "осмотреть телегу", defReduction = 0,
                outcome   = "Под сиденьем спрятан [noun].",
                rewardKeys = new[] { "map", "dagger" },
                poolKeys   = new[] { "map", "dagger", "rations" },
                weight    = 1.2f
            },
            new EventData {
                key       = "Guard",
                eventText = "Стражник патрулирует дорогу. Он требует объяснить, куда ты идёшь.",
                penalty   = -10,
                defStart  = "Молча", defHp = 0, defPow = 2, defSan = 1,
                defEnd    = "объяснить", defReduction = 0,
                outcome   = "Стражник кивает и передаёт [noun].",
                rewardKeys = new[] { "shield", "torch" },
                poolKeys   = new[] { "shield", "torch", "sword" },
                weight    = 1f
            },
            new EventData {
                key       = "MysteriousVoice",
                eventText = "Голос из-под земли обещает силу в обмен на жертву.",
                penalty   = -14,
                defStart  = "Молча", defHp = 1, defPow = 1, defSan = 3,
                defEnd    = "уйти", defReduction = 0,
                outcome   = "Голос стихает. На земле — [noun].",
                rewardKeys = new[] { "amulet", "poison" },
                poolKeys   = new[] { "amulet", "poison", "book" },
                weight    = 0.8f
            },
            new EventData {
                key       = "River",
                eventText = "Река вышла из берегов. Течение сильное, мост снесён.",
                penalty   = -11,
                defStart  = "Молча", defHp = 1, defPow = 2, defSan = 0,
                defEnd    = "искать брод", defReduction = 0,
                outcome   = "На том берегу ты находишь [noun].",
                rewardKeys = new[] { "rations", "medicine" },
                poolKeys   = new[] { "rations", "medicine", "map" },
                weight    = 1f
            },
            new EventData {
                key       = "Trap",
                eventText = "Нога проваливается в охотничий капкан. Боль пронзает тело.",
                penalty   = -16,
                defStart  = "Рефлексивно", defHp = 3, defPow = 1, defSan = 1,
                defEnd    = "высвободиться", defReduction = 0,
                outcome   = "Рядом с капканом — [noun].",
                rewardKeys = new[] { "medicine", "dagger" },
                poolKeys   = new[] { "medicine", "dagger", "sword" },
                weight    = 0.9f
            },
        };

        // ── Меню ────────────────────────────────────────────────────────────

        [MenuItem("Tools/Story/Generate Event Assets")]
        public static void Generate()
        {
            const string folder  = "Assets/_Project/Data/Events";
            CreateFolder(folder);

            // Собираем все WordSO по ключу
            var wordMap = new Dictionary<string, WordSO>();
            foreach (var key in AllWordKeys)
            {
                string path = $"Assets/_Project/Data/Words/Word_{key}.asset";
                var w = AssetDatabase.LoadAssetAtPath<WordSO>(path);
                if (w != null) wordMap[key] = w;
                else Debug.LogWarning($"[EventAssetGenerator] WordSO не найден: {path}");
            }

            // База событий
            var dbGuids = AssetDatabase.FindAssets("t:EventDatabaseSO");
            EventDatabaseSO db = dbGuids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<EventDatabaseSO>(
                    AssetDatabase.GUIDToAssetPath(dbGuids[0]))
                : null;

            if (db != null) db.events.Clear();

            foreach (var data in Events)
            {
                string path = $"{folder}/Event_{data.key}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<EventSO>(path);
                var so = existing != null ? existing : ScriptableObject.CreateInstance<EventSO>();

                so.eventText     = data.eventText;
                so.totalPenalty  = data.penalty;

                so.defaultPhraseStart    = data.defStart;
                so.defaultHpWeight       = data.defHp;
                so.defaultPowWeight      = data.defPow;
                so.defaultSanWeight      = data.defSan;

                so.defaultPhraseEnd      = data.defEnd;
                so.defaultPenaltyReduction = data.defReduction;

                so.outcomeText = data.outcome;
                so.weight      = data.weight;

                // Reward pool
                so.rewardWordPool.Clear();
                if (data.rewardKeys != null)
                    foreach (var k in data.rewardKeys)
                        if (wordMap.TryGetValue(k, out var rw))
                            so.rewardWordPool.Add(rw);

                // Event word pool
                so.eventWordPool.Clear();
                if (data.poolKeys != null)
                    foreach (var k in data.poolKeys)
                        if (wordMap.TryGetValue(k, out var pw))
                            so.eventWordPool.Add(pw);

                if (existing == null)
                    AssetDatabase.CreateAsset(so, path);
                else
                    EditorUtility.SetDirty(so);

                if (db != null) db.events.Add(so);
            }

            if (db != null) EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[EventAssetGenerator] Создано/обновлено {Events.Length} событий в {folder}");
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
