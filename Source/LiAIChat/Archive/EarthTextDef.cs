using Verse;

namespace LiAIChat.Archive
{
    public class EarthTextDef : Def
    {
        /// <summary>
        /// Original / commonly used English title.
        /// Example: The Republic
        /// </summary>
        public string title;

        /// <summary>
        /// Chinese display title.
        /// Example: 理想国
        /// </summary>
        public string titleChinese;

        /// <summary>
        /// Better to keep within 100 characters. To show in the bottom-left intro box.
        /// </summary>
        public string shortDescription;

        /// <summary>
        /// Author display name.
        /// Example: Plato
        /// </summary>
        public string author;

        /// <summary>
        /// Approximate year of composition/publication.
        ///
        /// Negative = BC
        /// Positive = AD
        ///
        /// Example:
        /// -375 = c. 375 BC
        /// 1781 = AD 1781
        /// </summary>
        public int year;

        /// <summary>
        /// Primary Knowledge topic associated with this text.
        ///
        /// Example:
        /// earth.philosophy.plato
        /// </summary>
        public string primaryTopicId;

        public string YearDisplay
        {
            get
            {
                if (year < 0)
                {
                    return "c. " + (-year) + " BC";
                }

                if (year > 0)
                {
                    return "AD " + year;
                }

                return "Unknown date";
            }
        }
    }
}