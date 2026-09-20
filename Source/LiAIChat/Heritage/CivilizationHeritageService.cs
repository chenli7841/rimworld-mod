using LiAIChat.Background;
using LiAIChat.Archive;
using LiAIChat.Config;
using LiAIChat.Game;
using LiAIChat.Knowledge;
using LiAIChat.Models;
using LiAIChat.State;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Verse;

namespace LiAIChat.Heritage
{
    public static class CivilizationHeritageService
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static void TryGenerate(Pawn creator, Thing product)
        {
            if (creator == null || product == null ||
                creator.Faction != Faction.OfPlayer ||
                !IsEligibleProduct(product))
            {
                return;
            }

            CompQuality qualityComp = product.TryGetComp<CompQuality>();

            if (qualityComp == null ||
                (qualityComp.Quality != QualityCategory.Masterwork &&
                 qualityComp.Quality != QualityCategory.Legendary))
            {
                return;
            }

            PawnAIState state = PawnAIStateManager.GetState(creator);

            if (state == null)
            {
                return;
            }

            List<string> topics = GetRelevantTopics(state);
            List<string> texts = GetRelevantTexts(state);

            if (topics.Count == 0 && texts.Count == 0)
            {
                return;
            }

            LiAIChatGameComponent component =
                Current.Game?.GetComponent<LiAIChatGameComponent>();

            if (component == null ||
                component.GetCivilizationHeritageWork(product.thingIDNumber) != null)
            {
                return;
            }

            CivilizationHeritageWork work = new CivilizationHeritageWork
            {
                ThingId = product.thingIDNumber,
                CreatorPawnId = creator.thingIDNumber,
                CreatorName = creator.LabelShort,
                CreatedTick = Find.TickManager == null ? -1 : Find.TickManager.TicksGame,
                Pending = true
            };

            component.CivilizationHeritageWorks.Add(work);

            string runtimeGameId = component.RuntimeGameId.ToString();
            string productLabel = product.LabelCap.ToString();
            string productKind = GetProductKind(product);
            string materialLabel = product.Stuff == null ? "无" : product.Stuff.label;
            string qualityLabel = GetQualityLabel(qualityComp.Quality);

            _ = GenerateAsync(
                runtimeGameId,
                work.ThingId,
                creator.LabelShort,
                productLabel,
                productKind,
                materialLabel,
                qualityLabel,
                topics,
                texts);
        }

        public static CivilizationHeritageWork GetWork(Thing thing)
        {
            if (thing == null || Current.Game == null)
            {
                return null;
            }

            LiAIChatGameComponent component =
                Current.Game.GetComponent<LiAIChatGameComponent>();

            return component == null
                ? null
                : component.GetCivilizationHeritageWork(thing.thingIDNumber);
        }

        private static async Task GenerateAsync(
            string runtimeGameId,
            int thingId,
            string creatorName,
            string productLabel,
            string productKind,
            string materialLabel,
            string qualityLabel,
            List<string> topics,
            List<string> texts)
        {
            try
            {
                OpenAICivilizationHeritageGenerator generator =
                    new OpenAICivilizationHeritageGenerator(
                        httpClient,
                        Config.Config.OpenAI_API_KEY);

                CivilizationHeritageText generated =
                    await generator.GenerateAsync(
                        creatorName,
                        productLabel,
                        productKind,
                        materialLabel,
                        qualityLabel,
                        topics,
                        texts);

                if (generated == null ||
                    string.IsNullOrWhiteSpace(generated.Description))
                {
                    MainThreadActionQueue.Enqueue(
                        () => RemovePendingWork(runtimeGameId, thingId));
                    return;
                }

                MainThreadActionQueue.Enqueue(
                    () => ApplyGeneratedWork(
                        runtimeGameId,
                        thingId,
                        generated));
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "[Li AI Chat] Civilization heritage generation failed: " +
                    ex.Message);

                MainThreadActionQueue.Enqueue(
                    () => RemovePendingWork(runtimeGameId, thingId));
            }
        }

        private static void ApplyGeneratedWork(
            string runtimeGameId,
            int thingId,
            CivilizationHeritageText generated)
        {
            LiAIChatGameComponent component = GetCurrentComponent(runtimeGameId);

            if (component == null)
            {
                return;
            }

            CivilizationHeritageWork work =
                component.GetCivilizationHeritageWork(thingId);

            if (work == null || !work.Pending)
            {
                return;
            }

            work.Title = generated.Title;
            work.Description = generated.Description;
            work.Pending = false;

            Messages.Message(
                "一件文明遗产作品的描述已经完成。",
                MessageTypeDefOf.PositiveEvent);
        }

        private static void RemovePendingWork(string runtimeGameId, int thingId)
        {
            LiAIChatGameComponent component = GetCurrentComponent(runtimeGameId);

            if (component == null)
            {
                return;
            }

            CivilizationHeritageWork work =
                component.GetCivilizationHeritageWork(thingId);

            if (work != null && work.Pending)
            {
                component.CivilizationHeritageWorks.Remove(work);
            }
        }

        private static LiAIChatGameComponent GetCurrentComponent(string runtimeGameId)
        {
            if (Current.Game == null)
            {
                return null;
            }

            LiAIChatGameComponent component =
                Current.Game.GetComponent<LiAIChatGameComponent>();

            return component != null &&
                component.RuntimeGameId.ToString() == runtimeGameId
                ? component
                : null;
        }

        private static bool IsEligibleProduct(Thing product)
        {
            return product.TryGetComp<CompArt>() != null ||
                product.def.IsWeapon ||
                product.def.defName == "DoubleBed" ||
                product.def.defName == "RoyalBed";
        }

        private static string GetProductKind(Thing product)
        {
            if (product.TryGetComp<CompArt>() != null)
            {
                return "艺术品";
            }

            if (product.def.IsWeapon)
            {
                return "武器";
            }

            return "豪华双人床";
        }

        private static List<string> GetRelevantTopics(PawnAIState state)
        {
            if (state.Knowledge?.KnownTopics == null)
            {
                return new List<string>();
            }

            return state.Knowledge.KnownTopics
                .Where(topic => topic != null && topic.Familiarity >= 0.15f)
                .OrderByDescending(topic => topic.Familiarity)
                .Take(3)
                .Select(topic => KnowledgeTopicCatalog.GetChineseDisplayName(topic.TopicId) +
                    "（" + topic.Familiarity.ToStringPercent() + "）")
                .ToList();
        }

        private static List<string> GetRelevantTexts(PawnAIState state)
        {
            return DefDatabase<EarthTextDef>.AllDefsListForReading
                .Where(text => text != null &&
                    state.GetEarthTextFamiliarity(text.defName) >= 0.15f)
                .OrderByDescending(text => state.GetEarthTextFamiliarity(text.defName))
                .Take(3)
                .Select(text =>
                {
                    string title = !string.IsNullOrWhiteSpace(text.titleChinese)
                        ? text.titleChinese
                        : (text.title ?? text.label);
                    return title + "（" +
                        state.GetEarthTextFamiliarity(text.defName).ToStringPercent() + "）";
                })
                .ToList();
        }

        private static string GetQualityLabel(QualityCategory quality)
        {
            return quality == QualityCategory.Legendary ? "传奇" : "大师";
        }
    }
}
