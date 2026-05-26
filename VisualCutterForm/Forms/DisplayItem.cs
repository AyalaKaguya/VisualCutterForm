namespace VisualMaster.Forms
{
    public class DisplayItem
    {
        public string Id { get; set; }
        public string Display { get; set; }
        public object Tag { get; set; }

        public override string ToString() => Display ?? Id ?? "";

        public DisplayItem() { }

        public DisplayItem(string id, string display)
        {
            Id = id;
            Display = display;
        }
    }
}
