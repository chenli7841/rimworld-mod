using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace LiAIChat.Archive
{
    public class ArchiveReflection : IExposable
    {
        public string ContentDefName;
        public string Text;
        public int CreatedTick;

        public void ExposeData()
        {
            Scribe_Values.Look(
                ref ContentDefName,
                "contentDefName");

            Scribe_Values.Look(
                ref Text,
                "text");

            Scribe_Values.Look(
                ref CreatedTick,
                "createdTick");
        }
    }
}
