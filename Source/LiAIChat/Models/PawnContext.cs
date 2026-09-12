using System.Collections.Generic;

namespace LiAIChat.Models
{
    public class PawnContext
    {
        public string Name { get; set; }

        public string Gender { get; set; }

        public int Age { get; set; }

        public List<string> Traits { get; set; }
            = new List<string>();

        public string Childhood { get; set; }

        public string Adulthood { get; set; }

        public int CurrentGameTick { get; set; }
    }
}