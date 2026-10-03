using System;
using Game.Table;

namespace Game.GardenCare
{
    public static class GardenCareSettings
    {
        public static string Fingerprint() => MiniGameTableData.Fingerprint("garden_care");
        public static string VisitVariable(int plot) => "ch2_gardenVisits_"+plot;
        public static GardenCareConfig Load(int visit = 1)
        {
            var rows=MiniGameTableData.Load<GardenCareSettingsRow>("garden_care");
            var row=MiniGameTableData.SelectLevel(rows,visit,r=>r.Level);
            foreach(var r in rows)ToConfig(r).Validate();
            return ToConfig(row);
        }
        private static GardenCareConfig ToConfig(GardenCareSettingsRow r)
        {
            var c=new GardenCareConfig{cropCount=r.CropCount,weedCount=r.WeedCount,insectCount=r.InsectCount,
                columns=r.Columns,rows=r.Rows,timeLimitSeconds=r.TimeLimitSeconds,resultSeconds=r.ResultSeconds,
                tutorialHintDelay=r.TutorialHintDelay,requestSeconds=r.RequestSeconds,tutorial=r.Level==1};
            c.Validate(); return c;
        }
    }
}
