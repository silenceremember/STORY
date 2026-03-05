#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Story.Data;

namespace Story.Editor
{
    /// <summary>
    /// Tools → Story → Generate Event Assets
    /// 20 событий под систему Verb+Noun + Nature (positive/negative outcomes).
    /// </summary>
    public static class EventAssetGenerator
    {
        private struct EventData
        {
            public string key;
            public string eventText;
            public int    penalty;
            public string defStart;
            public string defPhrasePast;
            public float  defHp, defPow, defSan;
            public string defEnd;
            public int    defReduction;
            // Outcome
            public string[] favorableKeys;
            public string positiveOutcome;
            public string negativeOutcome;
            public string[] rewardKeys;
            public string[] poolKeys;
            public float  weight;
        }

        private static readonly string[] AllWordKeys = {
            "hurl","throw","give","show","use","hide","toss","offer","grab","raise",
            "sword","shield","torch","medicine","map","rations","amulet","poison","book","dagger"
        };

        private static readonly EventData[] Events = new[]
        {
            new EventData {
                key       = "Bandits",
                eventText = "Разбойники перегородили тропу. Главарь ухмыляется, поигрывая ножом.",
                penalty   = -12,
                defStart  = "Обойти", defPhrasePast = "обходишь",
                defHp = 1, defPow = 1, defSan = 1,
                defEnd    = "разбойников", defReduction = 0,
                favorableKeys = new[] { "sword", "dagger", "shield", "hurl", "throw", "grab", "toss" },
                positiveOutcome = "и разбойники отступают. Ты находишь [noun] у дороги.",
                negativeOutcome = "но разбойники лишь злятся. Ты еле уносишь ноги.",
                rewardKeys = new[] { "dagger", "shield" },
                poolKeys   = new[] { "sword", "dagger", "shield" },
                weight    = 1f
            },
            new EventData {
                key       = "DarkCave",
                eventText = "Тёмная пещера зияет перед тобой. Изнутри доносится капель и далёкий рык.",
                penalty   = -14,
                defStart  = "Осмотреть", defPhrasePast = "осматриваешь",
                defHp = 1, defPow = 2, defSan = 1,
                defEnd    = "пещеру", defReduction = 0,
                favorableKeys = new[] { "torch", "map", "amulet", "show", "use", "raise" },
                positiveOutcome = "и в глубине мерцает [noun]. Ты берёшь находку.",
                negativeOutcome = "но тьма поглощает тебя. Ты выбираешься с пустыми руками.",
                rewardKeys = new[] { "torch", "amulet" },
                poolKeys   = new[] { "torch", "map", "amulet" },
                weight    = 1f
            },
            new EventData {
                key       = "WoundedMonk",
                eventText = "У обочины лежит раненый монах. Он протягивает руку и шепчет просьбу.",
                penalty   = -10,
                defStart  = "Обойти", defPhrasePast = "обходишь",
                defHp = 2, defPow = 1, defSan = 0,
                defEnd    = "монаха", defReduction = 0,
                favorableKeys = new[] { "medicine", "rations", "book", "give", "offer", "show" },
                positiveOutcome = "и монах благодарит, оставляя тебе [noun].",
                negativeOutcome = "но монах отшатывается в ужасе. Ты уходишь ни с чем.",
                rewardKeys = new[] { "medicine", "book" },
                poolKeys   = new[] { "medicine", "book", "amulet" },
                weight    = 1f
            },
            new EventData {
                key       = "StoneGolem",
                eventText = "Каменный голем преграждает мост. Его глаза пылают алым.",
                penalty   = -18,
                defStart  = "Обойти", defPhrasePast = "обходишь",
                defHp = 2, defPow = 2, defSan = 1,
                defEnd    = "голема", defReduction = 0,
                favorableKeys = new[] { "sword", "dagger", "hurl", "throw", "toss", "grab" },
                positiveOutcome = "и голем рассыпается. Среди обломков — [noun].",
                negativeOutcome = "но голем наносит удар. Ты проскакиваешь, однако ранен.",
                rewardKeys = new[] { "shield", "sword" },
                poolKeys   = new[] { "sword", "shield", "dagger" },
                weight    = 0.8f
            },
            new EventData {
                key       = "ForestSpirits",
                eventText = "Лесные духи танцуют в лунном свете. Они манят тебя присоединиться.",
                penalty   = -11,
                defStart  = "Осмотреть", defPhrasePast = "осматриваешь",
                defHp = 0, defPow = 1, defSan = 3,
                defEnd    = "духов", defReduction = 0,
                favorableKeys = new[] { "amulet", "book", "torch", "show", "offer", "give" },
                positiveOutcome = "и духи оставляют подарок — [noun].",
                negativeOutcome = "но духи разъярены. Лес замолкает, и ты остаёшься один.",
                rewardKeys = new[] { "amulet", "book" },
                poolKeys   = new[] { "amulet", "book", "torch" },
                weight    = 1f
            },
            new EventData {
                key       = "PoisonedWell",
                eventText = "Колодец у деревни отравлен. Жители смотрят с мольбой.",
                penalty   = -13,
                defStart  = "Осмотреть", defPhrasePast = "осматриваешь",
                defHp = 1, defPow = 2, defSan = 1,
                defEnd    = "колодец", defReduction = 0,
                favorableKeys = new[] { "medicine", "give", "offer", "use" },
                positiveOutcome = "и колодец очищен. Староста дарит тебе [noun].",
                negativeOutcome = "но жители в ужасе прогоняют тебя. Колодец остаётся отравлен.",
                rewardKeys = new[] { "medicine", "rations" },
                poolKeys   = new[] { "medicine", "poison", "rations" },
                weight    = 1f
            },
            new EventData {
                key       = "Merchant",
                eventText = "Бродячий торговец раскладывает товары. Цены кусаются.",
                penalty   = -8,
                defStart  = "Осмотреть", defPhrasePast = "осматриваешь",
                defHp = 1, defPow = 1, defSan = 0,
                defEnd    = "товары", defReduction = 0,
                favorableKeys = new[] { "rations", "map", "amulet", "offer", "give", "show" },
                positiveOutcome = "и торговец уходит, оставив [noun] в подарок.",
                negativeOutcome = "но торговец поспешно собирает товары и убегает.",
                rewardKeys = new[] { "map", "rations" },
                poolKeys   = new[] { "map", "rations", "dagger" },
                weight    = 1.2f
            },
            new EventData {
                key       = "Nightmare",
                eventText = "Ночной кошмар не отпускает. Тени сгущаются, шепот нарастает.",
                penalty   = -15,
                defStart  = "Перетерпеть", defPhrasePast = "терпишь",
                defHp = 0, defPow = 1, defSan = 3,
                defEnd    = "кошмар", defReduction = 0,
                favorableKeys = new[] { "torch", "amulet", "book", "use", "raise", "show" },
                positiveOutcome = "и рассвет приносит облегчение. Рядом — [noun].",
                negativeOutcome = "но кошмар затягивает глубже. Ты просыпаешься разбитым.",
                rewardKeys = new[] { "book", "amulet" },
                poolKeys   = new[] { "book", "amulet", "torch" },
                weight    = 1f
            },
            new EventData {
                key       = "BridgeTroll",
                eventText = "Тролль под мостом требует плату за проход. Мост — единственный путь.",
                penalty   = -14,
                defStart  = "Обойти", defPhrasePast = "обходишь",
                defHp = 1, defPow = 2, defSan = 1,
                defEnd    = "тролля", defReduction = 0,
                favorableKeys = new[] { "rations", "amulet", "give", "offer", "toss" },
                positiveOutcome = "и тролль хохочет, швыряя тебе [noun].",
                negativeOutcome = "но тролль рычит и едва не раздавливает тебя. Ты перебегаешь мост.",
                rewardKeys = new[] { "dagger", "poison" },
                poolKeys   = new[] { "sword", "dagger", "poison" },
                weight    = 0.9f
            },
            new EventData {
                key       = "Ruins",
                eventText = "Древние руины скрывают забытые знания. Стены покрыты письменами.",
                penalty   = -10,
                defStart  = "Изучить", defPhrasePast = "изучаешь",
                defHp = 0, defPow = 1, defSan = 2,
                defEnd    = "письмена", defReduction = 0,
                favorableKeys = new[] { "book", "map", "torch", "show", "use" },
                positiveOutcome = "и среди камней ты находишь [noun].",
                negativeOutcome = "но руины содрогаются. Ты выбегаешь до обвала.",
                rewardKeys = new[] { "book", "map" },
                poolKeys   = new[] { "book", "map", "amulet" },
                weight    = 1f
            },
            new EventData {
                key       = "Ambush",
                eventText = "Засада! Стрелы свистят над головой. Укрытие — за ближайшим валуном.",
                penalty   = -16,
                defStart  = "Осмотреть", defPhrasePast = "осматриваешь",
                defHp = 2, defPow = 2, defSan = 1,
                defEnd    = "укрытие", defReduction = 0,
                favorableKeys = new[] { "sword", "shield", "dagger", "hurl", "throw", "grab", "toss" },
                positiveOutcome = "и нападавшие бегут. Ты подбираешь [noun].",
                negativeOutcome = "но стрелы продолжают лететь. Ты отступаешь с потерями.",
                rewardKeys = new[] { "shield", "sword" },
                poolKeys   = new[] { "shield", "sword", "dagger" },
                weight    = 0.9f
            },
            new EventData {
                key       = "SickVillage",
                eventText = "Деревня охвачена мором. Больные лежат на улицах.",
                penalty   = -12,
                defStart  = "Обойти", defPhrasePast = "обходишь",
                defHp = 2, defPow = 1, defSan = 1,
                defEnd    = "деревню", defReduction = 0,
                favorableKeys = new[] { "medicine", "rations", "give", "offer" },
                positiveOutcome = "и знахарка благодарит, вручая [noun].",
                negativeOutcome = "но жители кричат тебе вслед. Мор не отступает.",
                rewardKeys = new[] { "medicine", "rations" },
                poolKeys   = new[] { "medicine", "rations", "poison" },
                weight    = 1f
            },
            new EventData {
                key       = "DesertedCamp",
                eventText = "Покинутый лагерь. Костёр ещё тёплый, вещи разбросаны.",
                penalty   = -9,
                defStart  = "Обыскать", defPhrasePast = "обыскиваешь",
                defHp = 1, defPow = 1, defSan = 1,
                defEnd    = "лагерь", defReduction = 0,
                favorableKeys = new[] { "torch", "map", "use", "grab", "show" },
                positiveOutcome = "и в палатке ты находишь [noun].",
                negativeOutcome = "но хозяева лагеря возвращаются. Ты уходишь ни с чем.",
                rewardKeys = new[] { "rations", "torch" },
                poolKeys   = new[] { "rations", "torch", "map" },
                weight    = 1.1f
            },
            new EventData {
                key       = "Portal",
                eventText = "Мерцающий портал висит в воздухе. Из него веет холодом иного мира.",
                penalty   = -17,
                defStart  = "Осмотреть", defPhrasePast = "осматриваешь",
                defHp = 1, defPow = 1, defSan = 3,
                defEnd    = "портал", defReduction = 0,
                favorableKeys = new[] { "amulet", "book", "use", "show", "raise" },
                positiveOutcome = "и портал схлопывается, выбросив [noun].",
                negativeOutcome = "но портал втягивает часть твоей силы и исчезает.",
                rewardKeys = new[] { "amulet", "book" },
                poolKeys   = new[] { "amulet", "book", "poison" },
                weight    = 0.7f
            },
            new EventData {
                key       = "HungryWolves",
                eventText = "Стая голодных волков окружает тебя. Вожак скалит зубы.",
                penalty   = -15,
                defStart  = "Обойти", defPhrasePast = "обходишь",
                defHp = 2, defPow = 2, defSan = 1,
                defEnd    = "стаю", defReduction = 0,
                favorableKeys = new[] { "sword", "dagger", "rations", "hurl", "throw", "toss", "grab" },
                positiveOutcome = "и волки убегают. У логова — [noun].",
                negativeOutcome = "но волки рычат громче. Ты отступаешь с трудом.",
                rewardKeys = new[] { "dagger", "rations" },
                poolKeys   = new[] { "sword", "dagger", "rations" },
                weight    = 1f
            },
            new EventData {
                key       = "AbandonedCart",
                eventText = "Телега стоит поперёк дороги. Лошади нет, борта изломаны.",
                penalty   = -7,
                defStart  = "Осмотреть", defPhrasePast = "осматриваешь",
                defHp = 1, defPow = 1, defSan = 0,
                defEnd    = "телегу", defReduction = 0,
                favorableKeys = new[] { "map", "torch", "use", "grab" },
                positiveOutcome = "и под сиденьем спрятан [noun].",
                negativeOutcome = "но телега пуста. Лишь скрип колёс на ветру.",
                rewardKeys = new[] { "map", "dagger" },
                poolKeys   = new[] { "map", "dagger", "rations" },
                weight    = 1.2f
            },
            new EventData {
                key       = "Guard",
                eventText = "Стражник патрулирует дорогу. Он требует объяснить, куда ты идёшь.",
                penalty   = -10,
                defStart  = "Осмотреть", defPhrasePast = "осматриваешь",
                defHp = 0, defPow = 2, defSan = 1,
                defEnd    = "стражника", defReduction = 0,
                favorableKeys = new[] { "map", "book", "show", "offer", "give" },
                positiveOutcome = "и стражник кивает, передавая [noun].",
                negativeOutcome = "но стражник хватается за оружие. Ты уходишь поспешно.",
                rewardKeys = new[] { "shield", "torch" },
                poolKeys   = new[] { "shield", "torch", "sword" },
                weight    = 1f
            },
            new EventData {
                key       = "MysteriousVoice",
                eventText = "Голос из-под земли обещает силу в обмен на жертву.",
                penalty   = -14,
                defStart  = "Осмотреть", defPhrasePast = "осматриваешь",
                defHp = 1, defPow = 1, defSan = 3,
                defEnd    = "разлом", defReduction = 0,
                favorableKeys = new[] { "amulet", "book", "use", "show", "raise" },
                positiveOutcome = "и голос стихает. На земле — [noun].",
                negativeOutcome = "но голос хохочет и замолкает. Земля дрожит под ногами.",
                rewardKeys = new[] { "amulet", "poison" },
                poolKeys   = new[] { "amulet", "poison", "book" },
                weight    = 0.8f
            },
            new EventData {
                key       = "River",
                eventText = "Река вышла из берегов. Течение сильное, мост снесён.",
                penalty   = -11,
                defStart  = "Осмотреть", defPhrasePast = "осматриваешь",
                defHp = 1, defPow = 2, defSan = 0,
                defEnd    = "берег", defReduction = 0,
                favorableKeys = new[] { "map", "rations", "use", "grab", "raise" },
                positiveOutcome = "и на том берегу ты находишь [noun].",
                negativeOutcome = "но течение сносит тебя. Ты выбираешься мокрый и без находок.",
                rewardKeys = new[] { "rations", "medicine" },
                poolKeys   = new[] { "rations", "medicine", "map" },
                weight    = 1f
            },
            new EventData {
                key       = "Trap",
                eventText = "Нога проваливается в охотничий капкан. Боль пронзает тело.",
                penalty   = -16,
                defStart  = "Осмотреть", defPhrasePast = "осматриваешь",
                defHp = 3, defPow = 1, defSan = 1,
                defEnd    = "капкан", defReduction = 0,
                favorableKeys = new[] { "medicine", "dagger", "use", "grab" },
                positiveOutcome = "и рядом с капканом — [noun].",
                negativeOutcome = "но капкан оставил глубокую рану. Ничего рядом нет.",
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

            var wordMap = new Dictionary<string, WordSO>();
            foreach (var key in AllWordKeys)
            {
                string path = $"Assets/_Project/Data/Words/Word_{key}.asset";
                var w = AssetDatabase.LoadAssetAtPath<WordSO>(path);
                if (w != null) wordMap[key] = w;
                else Debug.LogWarning($"[EventAssetGenerator] WordSO не найден: {path}");
            }

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
                so.defaultPhrasePast     = data.defPhrasePast ?? "";
                so.defaultHpWeight       = data.defHp;
                so.defaultPowWeight      = data.defPow;
                so.defaultSanWeight      = data.defSan;

                so.defaultPhraseEnd        = data.defEnd;
                so.defaultPenaltyReduction = data.defReduction;

                so.favorableWords.Clear();
                if (data.favorableKeys != null)
                    so.favorableWords.AddRange(data.favorableKeys);
                so.positiveOutcomeText = data.positiveOutcome;
                so.negativeOutcomeText = data.negativeOutcome;

                so.weight = data.weight;

                so.rewardWordPool.Clear();
                if (data.rewardKeys != null)
                    foreach (var k in data.rewardKeys)
                        if (wordMap.TryGetValue(k, out var rw))
                            so.rewardWordPool.Add(rw);

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
