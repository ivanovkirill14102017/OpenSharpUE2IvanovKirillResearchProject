namespace L2Viewer.DbFile.DbJson.Models
{
    public sealed class NpcRow
    {
        public decimal id { get; set; }
        public int idTemplate { get; set; }
        public string name { get; set; }
        public string @class { get; set; }
        public decimal collision_radius { get; set; }
        public decimal collision_height { get; set; }
        public decimal level { get; set; }
        public string sex { get; set; }
        public string type { get; set; }
        public string attackrange { get; set; }
        public decimal rhand { get; set; }
        public decimal lhand { get; set; }
        public decimal armor { get; set; }
        public NpcRowAbsorbType absorb_type { get; set; }
        public enum NpcRowAbsorbType
        {
            LAST_HIT,
            PARTY_ONE_RANDOM,
            FULL_PARTY
        }

    }
}
